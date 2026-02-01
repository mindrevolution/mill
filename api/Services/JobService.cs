using System.Collections.Concurrent;
using System.Threading.Channels;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Manages background job queues for long-running LLM operations.
/// </summary>
public class JobService : IDisposable
{
    private readonly ILogger<JobService> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    private readonly ConcurrentDictionary<string, Job> _jobs = new();
    private readonly Channel<Job> _llmQueue;
    private readonly Channel<Job> _shipQueue;
    private readonly List<CancellationTokenSource> _runningCancellations = [];
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellations = new();

    // SSE subscribers
    private readonly ConcurrentBag<Channel<JobEvent>> _subscribers = [];

    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _llmWorker;
    private readonly Task _shipWorker;

    public int LlmConcurrency { get; }
    public int ShipConcurrency { get; }

    public JobService(ILogger<JobService> logger, IServiceProvider services, IConfiguration config)
    {
        _logger = logger;
        _services = services;
        _config = config;

        LlmConcurrency = config.GetValue("Mill:Jobs:LlmConcurrency", 2);
        ShipConcurrency = config.GetValue("Mill:Jobs:ShipConcurrency", 4);

        _llmQueue = Channel.CreateUnbounded<Job>();
        _shipQueue = Channel.CreateUnbounded<Job>();

        // Start worker tasks
        _llmWorker = Task.Run(() => ProcessQueueAsync(_llmQueue, LlmConcurrency, _shutdownCts.Token));
        _shipWorker = Task.Run(() => ProcessQueueAsync(_shipQueue, ShipConcurrency, _shutdownCts.Token));
    }

    public string Enqueue(CreateJobRequest request)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var queue = GetQueueForType(request.Type);
        var title = GetTitleForJob(request.Type, request.Params);

        var job = new Job(
            Id: id,
            Type: request.Type,
            Queue: queue,
            Status: JobStatus.Queued,
            Title: title,
            Params: request.Params,
            Stage: null,
            ProgressPercent: null,
            Result: null,
            Error: null,
            SourceWorkspace: request.SourceWorkspace,
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null
        );

        _jobs[id] = job;
        _logger.LogInformation("Job {JobId} enqueued: {Type} - {Title}", id, request.Type, title);

        // Notify subscribers
        BroadcastEvent("job-created", job);

        // Add to appropriate queue
        var channel = queue == JobQueue.Llm ? _llmQueue : _shipQueue;
        channel.Writer.TryWrite(job);

        return id;
    }

    public Job? Get(string id) => _jobs.GetValueOrDefault(id);

    public IEnumerable<Job> GetAll() => _jobs.Values.OrderByDescending(j => j.CreatedAt);

    public bool Cancel(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            return false;

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Cancelled)
            return false;

        // Cancel if running
        if (_jobCancellations.TryRemove(id, out var cts))
        {
            cts.Cancel();
        }

        var cancelled = job with
        {
            Status = JobStatus.Cancelled,
            CompletedAt = DateTime.UtcNow
        };
        _jobs[id] = cancelled;

        _logger.LogInformation("Job {JobId} cancelled", id);
        BroadcastEvent("job-cancelled", cancelled);

        return true;
    }

    public void ClearCompleted()
    {
        var toRemove = _jobs.Values
            .Where(j => j.Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled)
            .Select(j => j.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _jobs.TryRemove(id, out _);
        }
    }

    public Channel<JobEvent> Subscribe()
    {
        var channel = Channel.CreateUnbounded<JobEvent>();
        _subscribers.Add(channel);
        return channel;
    }

    private void BroadcastEvent(string eventType, Job job)
    {
        var evt = new JobEvent(eventType, job);
        foreach (var subscriber in _subscribers)
        {
            subscriber.Writer.TryWrite(evt);
        }
    }

    private async Task ProcessQueueAsync(Channel<Job> queue, int concurrency, CancellationToken ct)
    {
        var semaphore = new SemaphoreSlim(concurrency);
        var tasks = new List<Task>();

        await foreach (var job in queue.Reader.ReadAllAsync(ct))
        {
            await semaphore.WaitAsync(ct);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ExecuteJobAsync(job, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);

            // Clean up completed tasks
            tasks.RemoveAll(t => t.IsCompleted);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteJobAsync(Job job, CancellationToken shutdownToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
        _jobCancellations[job.Id] = cts;

        try
        {
            // Update to running
            var running = job with
            {
                Status = JobStatus.Running,
                StartedAt = DateTime.UtcNow,
                Stage = "Starting..."
            };
            _jobs[job.Id] = running;
            BroadcastEvent("job-started", running);

            // Execute based on type
            object? result = null;
            try
            {
                result = job.Type switch
                {
                    JobType.DraftValidation => await ExecuteDraftValidationAsync(job, cts.Token),
                    JobType.ContextWarmup => await ExecuteContextWarmupAsync(job, cts.Token),
                    JobType.Kickstart => await ExecuteKickstartAsync(job, cts.Token),
                    JobType.ShipRun => await ExecuteShipRunAsync(job, cts.Token),
                    JobType.ObservationExtraction => await ExecuteObservationExtractionAsync(job, cts.Token),
                    _ => throw new NotSupportedException($"Unknown job type: {job.Type}")
                };

                // Complete successfully
                var completed = _jobs[job.Id] with
                {
                    Status = JobStatus.Completed,
                    Result = result,
                    Stage = null,
                    ProgressPercent = 100,
                    CompletedAt = DateTime.UtcNow
                };
                _jobs[job.Id] = completed;
                _logger.LogInformation("Job {JobId} completed successfully", job.Id);
                BroadcastEvent("job-completed", completed);
            }
            catch (OperationCanceledException)
            {
                // Already handled by Cancel()
                _logger.LogInformation("Job {JobId} was cancelled", job.Id);
            }
            catch (Exception ex)
            {
                var failed = _jobs[job.Id] with
                {
                    Status = JobStatus.Failed,
                    Error = ex.Message,
                    Stage = null,
                    CompletedAt = DateTime.UtcNow
                };
                _jobs[job.Id] = failed;
                _logger.LogError(ex, "Job {JobId} failed", job.Id);
                BroadcastEvent("job-failed", failed);
            }
        }
        finally
        {
            _jobCancellations.TryRemove(job.Id, out _);
            cts.Dispose();
        }
    }

    private void UpdateProgress(string jobId, string stage, int? percent = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var updated = job with { Stage = stage, ProgressPercent = percent };
            _jobs[jobId] = updated;
            BroadcastEvent("job-progress", updated);
        }
    }

    private async Task<DraftValidationResponse> ExecuteDraftValidationAsync(Job job, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var drafts = scope.ServiceProvider.GetRequiredService<DraftService>();

        var draftId = job.Params.GetValueOrDefault("draftId")?.ToString()
            ?? throw new ArgumentException("Missing draftId parameter");

        UpdateProgress(job.Id, "Loading draft...", 10);
        await Task.Delay(100, ct); // Small delay to let UI update

        UpdateProgress(job.Id, "Reading context...", 20);
        await Task.Delay(100, ct);

        UpdateProgress(job.Id, "Scanning codebase...", 40);

        // This calls the LLM - the long part
        var result = await drafts.ValidateRelevance(draftId);

        UpdateProgress(job.Id, "Finalizing...", 90);

        return result;
    }

    private async Task<object> ExecuteContextWarmupAsync(Job job, CancellationToken ct)
    {
        UpdateProgress(job.Id, "Analyzing project structure...", 20);
        await Task.Delay(500, ct);

        UpdateProgress(job.Id, "Scanning files...", 50);
        await Task.Delay(500, ct);

        UpdateProgress(job.Id, "Generating context...", 80);
        await Task.Delay(500, ct);

        // TODO: Implement actual context warmup
        return new { success = true, message = "Context warmup not yet implemented" };
    }

    private async Task<KickstartResponse> ExecuteKickstartAsync(Job job, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var ground = scope.ServiceProvider.GetRequiredService<GroundService>();
        var llmProvider = scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        UpdateProgress(job.Id, "Loading templates...", 10);

        var request = new KickstartRequest(
            Name: job.Params.GetValueOrDefault("name")?.ToString() ?? "Unnamed",
            Description: job.Params.GetValueOrDefault("description")?.ToString(),
            ArchetypeId: job.Params.GetValueOrDefault("archetypeId")?.ToString() ?? "",
            StackId: job.Params.GetValueOrDefault("stackId")?.ToString() ?? "",
            ArchetypeOverride: null,
            StackOverride: null
        );

        UpdateProgress(job.Id, "Generating product context...", 30);

        var result = await ground.RunKickstart(request, llmProvider);

        UpdateProgress(job.Id, "Writing files...", 90);

        return result;
    }

    private async Task<ShipRunResult> ExecuteShipRunAsync(Job job, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var ship = scope.ServiceProvider.GetRequiredService<ShipService>();
        var llmProvider = scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        var issueNumber = job.Params.GetValueOrDefault("issueNumber") switch
        {
            int n => n,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.Number => je.GetInt32(),
            _ => throw new ArgumentException("Missing or invalid issueNumber parameter")
        };

        var result = await ship.ExecuteShipRun(
            issueNumber,
            llmProvider,
            (stage, percent) => UpdateProgress(job.Id, stage, percent),
            ct
        );

        return result;
    }

    private async Task<ObservationExtractionResult> ExecuteObservationExtractionAsync(Job job, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var observations = scope.ServiceProvider.GetRequiredService<ObservationService>();
        var llmProvider = scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        // Parse params
        var issueNumber = job.Params.GetValueOrDefault("issueNumber") switch
        {
            int n => n,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            System.Text.Json.JsonElement je1 when je1.ValueKind == System.Text.Json.JsonValueKind.Number => je1.GetInt32(),
            _ => 0
        };

        var issueTitle = job.Params.GetValueOrDefault("issueTitle")?.ToString() ?? "";
        var specContent = job.Params.GetValueOrDefault("specContent")?.ToString() ?? "";

        var runSuccess = job.Params.GetValueOrDefault("runSuccess") switch
        {
            bool b => b,
            System.Text.Json.JsonElement je2 when je2.ValueKind == System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonElement je3 when je3.ValueKind == System.Text.Json.JsonValueKind.False => false,
            _ => false
        };

        var iterations = job.Params.GetValueOrDefault("iterations") switch
        {
            int n => n,
            long l => (int)l,
            System.Text.Json.JsonElement je4 when je4.ValueKind == System.Text.Json.JsonValueKind.Number => je4.GetInt32(),
            _ => 0
        };

        var iterationHistory = new List<ShipRunIteration>();
        if (job.Params.GetValueOrDefault("iterationHistory") is System.Text.Json.JsonElement historyJson
            && historyJson.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in historyJson.EnumerateArray())
            {
                var number = item.TryGetProperty("number", out var np) ? np.GetInt32() : 0;
                var signal = item.TryGetProperty("signal", out var sp) ? sp.GetString() ?? "" : "";
                var summary = item.TryGetProperty("summary", out var sump) ? sump.GetString() : null;
                var completedAt = item.TryGetProperty("completedAt", out var cap) && cap.TryGetDateTime(out var dt)
                    ? dt : DateTime.UtcNow;
                iterationHistory.Add(new ShipRunIteration(number, signal, summary, completedAt));
            }
        }

        var extractParams = new ObservationExtractionParams(
            IssueNumber: issueNumber,
            IssueTitle: issueTitle,
            SpecContent: specContent,
            RunSuccess: runSuccess,
            Iterations: iterations,
            IterationHistory: iterationHistory
        );

        return await observations.ExtractFromRun(
            extractParams,
            llmProvider,
            (stage, percent) => UpdateProgress(job.Id, stage, percent),
            ct
        );
    }

    private static JobQueue GetQueueForType(JobType type) => type switch
    {
        JobType.DraftValidation => JobQueue.Llm,
        JobType.ContextWarmup => JobQueue.Llm,
        JobType.Kickstart => JobQueue.Llm,
        JobType.ShipRun => JobQueue.Ship,
        JobType.ObservationExtraction => JobQueue.Llm,
        _ => JobQueue.Llm
    };

    private static string GetTitleForJob(JobType type, Dictionary<string, object?> p) => type switch
    {
        JobType.DraftValidation => $"Validate: {p.GetValueOrDefault("draftId")}",
        JobType.ContextWarmup => "Context warmup",
        JobType.Kickstart => $"Kickstart: {p.GetValueOrDefault("name")}",
        JobType.ShipRun => $"Ship: #{p.GetValueOrDefault("issueNumber")} - {p.GetValueOrDefault("issueTitle")}",
        JobType.ObservationExtraction => $"Extract observations: #{p.GetValueOrDefault("issueNumber")}",
        _ => type.ToString()
    };

    public void Dispose()
    {
        _shutdownCts.Cancel();
        _llmQueue.Writer.Complete();
        _shipQueue.Writer.Complete();

        foreach (var subscriber in _subscribers)
        {
            subscriber.Writer.Complete();
        }

        _shutdownCts.Dispose();
    }
}

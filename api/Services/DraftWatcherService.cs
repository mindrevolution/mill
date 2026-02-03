using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace MillApi.Services;

/// <summary>
/// Watches the drafts folder for changes and broadcasts events via SSE.
/// </summary>
public class DraftWatcherService : IHostedService, IDisposable
{
    private readonly ILogger<DraftWatcherService> _logger;
    private readonly ProjectContext _project;
    private readonly TimeSpan _debounceDelay = TimeSpan.FromSeconds(2);

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;
    private readonly ConcurrentBag<Channel<DraftEvent>> _subscribers = [];

    public DraftWatcherService(ILogger<DraftWatcherService> logger, ProjectContext project)
    {
        _logger = logger;
        _project = project;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Don't start watcher if no project is configured yet
        // It will be started when project is set
        if (!_project.IsConfigured)
        {
            _logger.LogInformation("DraftWatcher: No project configured, waiting...");
            _project.ProjectChanged += OnProjectChanged;
            return Task.CompletedTask;
        }

        StartWatcher();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopWatcher();
        _project.ProjectChanged -= OnProjectChanged;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopWatcher();
        _debounceCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    public Channel<DraftEvent> Subscribe()
    {
        var channel = Channel.CreateUnbounded<DraftEvent>();
        _subscribers.Add(channel);
        return channel;
    }

    private void OnProjectChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("DraftWatcher: Project changed, restarting watcher");
        StopWatcher();
        if (_project.IsConfigured)
        {
            StartWatcher();
        }
    }

    private void StartWatcher()
    {
        var draftsPath = Path.Combine(_project.MillFolder, "shape", "drafts");

        // Ensure directory exists
        if (!Directory.Exists(draftsPath))
        {
            Directory.CreateDirectory(draftsPath);
            _logger.LogInformation("DraftWatcher: Created drafts directory: {Path}", draftsPath);
        }

        _watcher = new FileSystemWatcher(draftsPath, "*.md")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        _watcher.Created += OnFileChange;
        _watcher.Changed += OnFileChange;
        _watcher.Deleted += OnFileChange;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;

        _logger.LogInformation("DraftWatcher: Started watching {Path}", draftsPath);
    }

    private void StopWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileChange;
            _watcher.Changed -= OnFileChange;
            _watcher.Deleted -= OnFileChange;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
            _logger.LogInformation("DraftWatcher: Stopped");
        }
    }

    private void OnFileChange(object sender, FileSystemEventArgs e)
    {
        DebounceBroadcast();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        DebounceBroadcast();
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "DraftWatcher: Error");
        // Try to restart the watcher
        StopWatcher();
        if (_project.IsConfigured)
        {
            StartWatcher();
        }
    }

    private void DebounceBroadcast()
    {
        // Cancel any pending debounce
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, token);
                if (!token.IsCancellationRequested)
                {
                    Broadcast();
                }
            }
            catch (OperationCanceledException)
            {
                // Debounce was cancelled by a newer event, that's fine
            }
        }, token);
    }

    private void Broadcast()
    {
        var evt = new DraftEvent("drafts-changed");

        foreach (var subscriber in _subscribers)
        {
            subscriber.Writer.TryWrite(evt);
        }
    }
}

public record DraftEvent(string EventType);

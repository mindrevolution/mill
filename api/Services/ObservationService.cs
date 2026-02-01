using System.Text.Json;
using System.Text.RegularExpressions;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service for observation extraction and management.
/// Observations are AI-generated suggestions for ground items, pending human review.
/// </summary>
public partial class ObservationService
{
    private readonly ILogger<ObservationService> _logger;
    private readonly ProjectContext _project;
    private readonly GroundService _ground;
    private readonly MillPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ObservationService(
        ILogger<ObservationService> logger,
        ProjectContext project,
        GroundService ground,
        MillPaths paths)
    {
        _logger = logger;
        _project = project;
        _ground = ground;
        _paths = paths;
    }

    // ─────────────────────────────────────────────────────────────────
    // Storage
    // ─────────────────────────────────────────────────────────────────

    private string ObservationsPath => Path.Combine(_project.MillFolder, "ground", "observations.json");

    private async Task<ObservationsFile> LoadFile()
    {
        if (!File.Exists(ObservationsPath))
            return new ObservationsFile([], []);

        try
        {
            var json = await File.ReadAllTextAsync(ObservationsPath);
            return JsonSerializer.Deserialize<ObservationsFile>(json, JsonOptions) ?? new ObservationsFile([], []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load observations.json");
            return new ObservationsFile([], []);
        }
    }

    private async Task SaveFile(ObservationsFile file)
    {
        var dir = Path.GetDirectoryName(ObservationsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(file, JsonOptions);
        await File.WriteAllTextAsync(ObservationsPath, json);
    }

    private record ObservationsFile(List<Observation> Observations, List<string> Banlist);

    // ─────────────────────────────────────────────────────────────────
    // CRUD
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<Observation>> GetAll()
    {
        var file = await LoadFile();
        return file.Observations;
    }

    public async Task<Observation?> Get(string id)
    {
        var file = await LoadFile();
        return file.Observations.FirstOrDefault(o => o.Id == id);
    }

    public async Task Add(Observation obs)
    {
        var file = await LoadFile();
        file.Observations.Add(obs);
        await SaveFile(file);
    }

    public async Task Update(Observation obs)
    {
        var file = await LoadFile();
        var idx = file.Observations.FindIndex(o => o.Id == obs.Id);
        if (idx >= 0)
        {
            file.Observations[idx] = obs;
            await SaveFile(file);
        }
    }

    public async Task Delete(string id)
    {
        var file = await LoadFile();
        file.Observations.RemoveAll(o => o.Id == id);
        await SaveFile(file);
    }

    public async Task<List<string>> GetBanlist()
    {
        var file = await LoadFile();
        return file.Banlist;
    }

    // ─────────────────────────────────────────────────────────────────
    // Actions
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Accept an observation: promotes it to a ground item.
    /// </summary>
    public async Task<KnowledgeItem?> Accept(string id)
    {
        var obs = await Get(id);
        if (obs == null) return null;

        // Parse suggestion into name and description
        var (name, description) = ParseSuggestion(obs.Suggestion);
        var filename = ToFilename(name);
        var filePath = Path.Combine(_project.MillFolder, "ground", obs.Category, $"{filename}.md");

        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Create the ground file
        var content = $"# {name}\n\n{description}\n";
        await File.WriteAllTextAsync(filePath, content);

        // Remove observation
        await Delete(id);

        _logger.LogInformation("Accepted observation {Id} as ground item: {Path}", id, filePath);

        return new KnowledgeItem(
            Id: filename,
            Category: obs.Category,
            Name: name,
            Description: description,
            File: $"{filename}.md",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Ban an observation: delete it and add its key terms to banlist.
    /// </summary>
    public async Task Ban(string id)
    {
        var obs = await Get(id);
        if (obs == null) return;

        var file = await LoadFile();

        // Extract key term from suggestion (the part before " — ")
        var (name, _) = ParseSuggestion(obs.Suggestion);
        var normalized = name.ToLowerInvariant().Trim();

        if (!file.Banlist.Contains(normalized))
            file.Banlist.Add(normalized);

        file.Observations.RemoveAll(o => o.Id == id);
        await SaveFile(file);

        _logger.LogInformation("Banned observation {Id}, added '{Term}' to banlist", id, normalized);
    }

    // ─────────────────────────────────────────────────────────────────
    // Extraction
    // ─────────────────────────────────────────────────────────────────

    public async Task<ObservationExtractionResult> ExtractFromRun(
        ObservationExtractionParams runParams,
        ILlmProvider llmProvider,
        Action<string, int?> onProgress,
        CancellationToken ct)
    {
        onProgress("Loading context...", 10);

        // Load existing data
        var existingFile = await LoadFile();
        var existingObservations = existingFile.Observations;
        var banlist = existingFile.Banlist;

        // Load existing ground items
        var groundItems = new List<KnowledgeItem>();
        foreach (var category in new[] { "personas", "standards", "concepts", "design" })
        {
            groundItems.AddRange(await _ground.GetItems(category));
        }

        onProgress("Building prompt...", 20);

        // Build and execute prompt
        var prompt = await BuildExtractionPrompt(runParams, existingObservations, groundItems, banlist);

        onProgress("Extracting observations...", 40);

        var response = await llmProvider.Execute(prompt, new LlmOptions(
            WorkingDir: _project.ProjectPath,
            TimeoutMs: 120000
        ));

        if (!response.Success)
        {
            _logger.LogError("Observation extraction failed: {Error}", response.Error);
            return new ObservationExtractionResult(0, 0, 0, 0);
        }

        onProgress("Processing results...", 80);

        // Parse extracted observations
        var extracted = ParseExtractionOutput(response.Output);
        var sourceRef = $"#{runParams.IssueNumber}";
        var now = DateTime.UtcNow;

        var added = 0;
        var updated = 0;
        var discarded = 0;

        foreach (var ext in extracted)
        {
            // Skip low confidence
            if (ext.Confidence < GetConfidenceThreshold())
            {
                discarded++;
                continue;
            }

            // Skip if matches ground item
            var matchesGround = groundItems.Any(g =>
                g.Category == ext.Category &&
                g.Name.Equals(ParseSuggestion(ext.Suggestion).name, StringComparison.OrdinalIgnoreCase));
            if (matchesGround)
            {
                discarded++;
                continue;
            }

            // Skip if banned
            var (name, _) = ParseSuggestion(ext.Suggestion);
            if (banlist.Any(b => name.ToLowerInvariant().Contains(b)))
            {
                discarded++;
                continue;
            }

            // Check if matches existing observation
            if (ext.MatchesExisting != null)
            {
                var existing = existingObservations.FirstOrDefault(o => o.Id == ext.MatchesExisting);
                if (existing != null)
                {
                    // Boost confidence and add source
                    var newSources = existing.Sources.ToList();
                    if (!newSources.Contains(sourceRef))
                        newSources.Add(sourceRef);

                    var boostedConfidence = CalculateBoostedConfidence(existing.Confidence);
                    var updatedObs = existing with
                    {
                        Sources = newSources,
                        Confidence = boostedConfidence,
                        UpdatedAt = now
                    };
                    await Update(updatedObs);
                    updated++;
                    continue;
                }
            }

            // Add new observation
            var newObs = new Observation(
                Id: $"obs_{Guid.NewGuid():N}"[..12],
                Category: ext.Category,
                Suggestion: ext.Suggestion,
                Sources: [sourceRef],
                Confidence: ext.Confidence,
                CreatedAt: now,
                UpdatedAt: now
            );
            await Add(newObs);
            added++;
        }

        onProgress("Done", 100);

        _logger.LogInformation(
            "Observation extraction complete: {Extracted} extracted, {Added} added, {Updated} updated, {Discarded} discarded",
            extracted.Count, added, updated, discarded);

        return new ObservationExtractionResult(extracted.Count, added, updated, discarded);
    }

    private async Task<string> BuildExtractionPrompt(
        ObservationExtractionParams runParams,
        List<Observation> existingObservations,
        List<KnowledgeItem> groundItems,
        List<string> banlist)
    {
        var templatePath = Path.Combine(_paths.ShipPrompts, "run-observations.md");
        if (!File.Exists(templatePath))
            throw new InvalidOperationException("run-observations.md prompt template not found");

        var template = await File.ReadAllTextAsync(templatePath);

        // Format iteration history
        var historyText = string.Join("\n", runParams.IterationHistory.Select(h =>
            $"- Iteration {h.Number}: {h.Signal} — {h.Summary ?? "no summary"}"));

        // Format existing ground items
        var groundText = string.Join("\n", groundItems.Select(g =>
            $"- [{g.Category}] {g.Name}: {g.Description}"));

        // Format existing observations
        var obsText = existingObservations.Count > 0
            ? string.Join("\n", existingObservations.Select(o =>
                $"- [{o.Id}] [{o.Category}] {o.Suggestion} (confidence: {o.Confidence:F2})"))
            : "None";

        // Format banlist
        var banlistText = banlist.Count > 0
            ? string.Join(", ", banlist)
            : "None";

        return template
            .Replace("{{SPEC_CONTENT}}", runParams.SpecContent)
            .Replace("{{RUN_SUCCESS}}", runParams.RunSuccess ? "Yes" : "No")
            .Replace("{{ITERATIONS}}", runParams.Iterations.ToString())
            .Replace("{{ITERATION_HISTORY}}", historyText)
            .Replace("{{EXISTING_GROUND}}", groundText)
            .Replace("{{EXISTING_OBSERVATIONS}}", obsText)
            .Replace("{{BANLIST}}", banlistText);
    }

    private record ExtractedObservation(
        string Category,
        string Suggestion,
        double Confidence,
        string? MatchesExisting
    );

    private List<ExtractedObservation> ParseExtractionOutput(string output)
    {
        // Look for JSON block in output
        var jsonMatch = JsonBlockRegex().Match(output);
        if (!jsonMatch.Success)
        {
            _logger.LogWarning("No JSON block found in extraction output");
            return [];
        }

        try
        {
            var json = jsonMatch.Groups[1].Value;
            var result = JsonSerializer.Deserialize<ExtractionResult>(json, JsonOptions);
            return result?.Observations ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse extraction JSON: {Error}", ex.Message);
            return [];
        }
    }

    [GeneratedRegex(@"```json\s*(\{[\s\S]*?\})\s*```", RegexOptions.Multiline)]
    private static partial Regex JsonBlockRegex();

    private record ExtractionResult(List<ExtractedObservation>? Observations);

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private double GetConfidenceThreshold()
    {
        // TODO: Read from .mill/project.json
        return 0.7;
    }

    /// <summary>
    /// Boost confidence when observation is reinforced by multiple runs.
    /// Each reinforcement nudges confidence up, asymptotic to 0.95.
    /// </summary>
    public static double CalculateBoostedConfidence(double current)
    {
        return Math.Min(0.95, current + (1 - current) * 0.2);
    }

    private static (string name, string description) ParseSuggestion(string suggestion)
    {
        var parts = suggestion.Split(" — ", 2);
        return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : "");
    }

    private static string ToFilename(string name)
    {
        // Convert "Enterprise Admin" to "enterprise-admin"
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}

using System.Text.Json;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service for shape workspace drafts.
/// </summary>
public class DraftService
{
    private readonly ILogger<DraftService> _logger;
    private readonly ProjectContext _project;
    private readonly MarkdownService _markdown;
    private readonly MillPaths _paths;
    private readonly ILlmProvider _llmProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DraftService(
        ILogger<DraftService> logger,
        ProjectContext project,
        MarkdownService markdown,
        MillPaths paths,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _project = project;
        _markdown = markdown;
        _paths = paths;
        _llmProvider = llmProvider;
    }

    private string DraftsPath => Path.Combine(_project.MillFolder, "shape", "drafts");

    public async Task<List<Draft>> GetAll()
    {
        if (!Directory.Exists(DraftsPath))
            return [];

        var drafts = new List<Draft>();
        foreach (var file in Directory.GetFiles(DraftsPath, "*.md"))
        {
            var info = new FileInfo(file);
            var slug = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var fm = ParseFrontmatter(content);

            drafts.Add(new Draft(
                Id: slug,
                Slug: slug,
                Title: fm.title ?? MarkdownHelpers.FormatName(slug),
                Type: fm.type ?? "feature",
                Status: fm.status ?? "draft",
                UpdatedAt: info.LastWriteTime,
                Persona: fm.persona
            ));
        }

        return drafts.OrderByDescending(d => d.UpdatedAt).ToList();
    }

    public async Task<DraftDetail?> Get(string id)
    {
        var filePath = Path.Combine(DraftsPath, $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseFrontmatter(content);
        var body = MarkdownHelpers.ExtractBody(content);

        return new DraftDetail(
            Id: id,
            Slug: id,
            Title: fm.title ?? MarkdownHelpers.FormatName(id),
            Body: body,
            BodyHtml: _markdown.ToHtml(body),
            Type: fm.type ?? "feature",
            Status: fm.status ?? "draft",
            UpdatedAt: info.LastWriteTime,
            Persona: fm.persona
        );
    }

    public async Task<DraftValidationResponse> ValidateRelevance(string draftId)
    {
        // Load draft
        var draftPath = Path.Combine(DraftsPath, $"{draftId}.md");
        if (!File.Exists(draftPath))
            throw new ArgumentException($"Draft not found: {draftId}");

        var draftContent = await File.ReadAllTextAsync(draftPath);

        // Load prompt template
        var promptPath = Path.Combine(_paths.ShapePrompts, "draft-validate.md");
        if (!File.Exists(promptPath))
            throw new InvalidOperationException($"Validation prompt not found: {promptPath}");

        var promptTemplate = await File.ReadAllTextAsync(promptPath);

        // Build prompt with draft content
        var prompt = promptTemplate.Replace("{{DRAFT_CONTENT}}", draftContent);

        _logger.LogInformation("Running draft validation for {DraftId}", draftId);

        // Execute via LLM
        var response = await _llmProvider.Execute(prompt, new LlmOptions(
            WorkingDir: _project.ProjectPath,
            TimeoutMs: 180000 // 3 minutes for validation
        ));

        if (!response.Success)
        {
            _logger.LogError("Draft validation failed: {Error}", response.Error);
            throw new InvalidOperationException($"Validation failed: {response.Error}");
        }

        // Parse JSON response
        var result = ParseValidationResponse(response.Output);
        _logger.LogInformation("Draft {DraftId} validation complete: score={Score}, verdict={Verdict}",
            draftId, result.Score, result.Verdict);

        return result;
    }

    private DraftValidationResponse ParseValidationResponse(string output)
    {
        // Extract JSON from response (may have extra whitespace or text)
        var jsonStart = output.IndexOf('{');
        var jsonEnd = output.LastIndexOf('}');

        if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
        {
            _logger.LogWarning("Failed to find JSON in validation response: {Output}", output[..Math.Min(500, output.Length)]);
            return new DraftValidationResponse(
                Score: 5,
                Verdict: "review",
                Findings: [],
                Summary: "Validation returned unexpected format. Manual review recommended.",
                Recommendation: "Review draft manually - automated validation could not parse result."
            );
        }

        var json = output[jsonStart..(jsonEnd + 1)];

        try
        {
            var parsed = JsonSerializer.Deserialize<DraftValidationResponse>(json, JsonOptions);
            return parsed ?? throw new JsonException("Deserialized to null");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse validation JSON: {Json}", json[..Math.Min(500, json.Length)]);
            return new DraftValidationResponse(
                Score: 5,
                Verdict: "review",
                Findings: [],
                Summary: "Validation returned malformed JSON. Manual review recommended.",
                Recommendation: "Review draft manually - automated validation result was unparseable."
            );
        }
    }

    private static (string? title, string? type, string? persona, string? status) ParseFrontmatter(string content)
    {
        string? title = null, type = null, persona = null, status = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter) break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter) continue;

            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "title": title = value; break;
                case "type": type = value; break;
                case "persona": persona = value; break;
                case "status": status = value; break;
            }
        }

        return (title, type, persona, status);
    }
}

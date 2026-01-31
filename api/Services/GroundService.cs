using System.Text;
using System.Text.Json;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service for ground workspace: items, templates, kickstart.
/// </summary>
public class GroundService
{
    private readonly ILogger<GroundService> _logger;
    private readonly ProjectContext _project;
    private readonly MillPaths _paths;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GroundService(
        ILogger<GroundService> logger,
        ProjectContext project,
        MillPaths paths)
    {
        _logger = logger;
        _project = project;
        _paths = paths;
    }

    // ─────────────────────────────────────────────────────────────────
    // Ground Items
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<KnowledgeItem>> GetItems(string category)
    {
        var groundPath = Path.Combine(_project.MillFolder, "ground", category);
        if (!Directory.Exists(groundPath))
            return [];

        var items = new List<KnowledgeItem>();
        foreach (var file in Directory.GetFiles(groundPath, "*.md"))
        {
            var info = new FileInfo(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var description = MarkdownHelpers.ExtractDescription(content);

            items.Add(new KnowledgeItem(
                Id: name,
                Category: category,
                Name: MarkdownHelpers.FormatName(name),
                Description: description,
                File: info.Name,
                CreatedAt: info.CreationTime,
                UpdatedAt: info.LastWriteTime
            ));
        }

        return items;
    }

    public async Task<string?> GetItemContent(string category, string id)
    {
        var filePath = Path.Combine(_project.MillFolder, "ground", category, $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllTextAsync(filePath);
    }

    public GroundStatus GetStatus()
    {
        var groundPath = Path.Combine(_project.MillFolder, "ground");
        var kickstartPath = Path.Combine(groundPath, "kickstart.json");

        var isEmpty = true;
        var categories = new[] { "personas", "standards", "concepts", "design" };
        foreach (var category in categories)
        {
            var categoryPath = Path.Combine(groundPath, category);
            if (Directory.Exists(categoryPath) && Directory.GetFiles(categoryPath, "*.md").Length > 0)
            {
                isEmpty = false;
                break;
            }
        }

        var hasKickstart = File.Exists(kickstartPath);
        return new GroundStatus(isEmpty, hasKickstart);
    }

    // ─────────────────────────────────────────────────────────────────
    // Templates
    // ─────────────────────────────────────────────────────────────────

    public List<TemplateSummary> GetArchetypes() => GetTemplates("archetypes");
    public List<TemplateSummary> GetStacks() => GetTemplates("stacks");

    private List<TemplateSummary> GetTemplates(string type)
    {
        var templatesPath = Path.Combine(_paths.Templates, type);
        if (!Directory.Exists(templatesPath))
            return [];

        var templates = new List<TemplateSummary>();
        foreach (var file in Directory.GetFiles(templatesPath, "*.md"))
        {
            var content = File.ReadAllText(file);
            var frontmatter = ParseTemplateFrontmatter(content);

            if (frontmatter.id != null)
            {
                templates.Add(new TemplateSummary(
                    frontmatter.id,
                    frontmatter.label ?? MarkdownHelpers.FormatName(frontmatter.id),
                    frontmatter.summary ?? "",
                    frontmatter.tags ?? []
                ));
            }
        }

        return templates.OrderBy(t => t.Label).ToList();
    }

    public TemplateContent? GetTemplate(string type, string id)
    {
        var filePath = Path.Combine(_paths.Templates, type, $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var content = File.ReadAllText(filePath);
        return new TemplateContent(id, type, content);
    }

    private static (string? id, string? label, string? summary, List<string>? tags) ParseTemplateFrontmatter(string content)
    {
        string? id = null, label = null, summary = null;
        List<string>? tags = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;
        var inTags = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter) break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter) continue;

            if (inTags)
            {
                if (line.TrimStart().StartsWith("- "))
                {
                    tags ??= [];
                    tags.Add(line.TrimStart()[2..].Trim());
                    continue;
                }
                inTags = false;
            }

            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "id": id = value; break;
                case "label": label = value; break;
                case "summary": summary = value; break;
                case "tags":
                    if (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        var tagContent = value[1..^1];
                        tags = tagContent.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        inTags = true;
                    }
                    break;
            }
        }

        return (id, label, summary, tags);
    }

    // ─────────────────────────────────────────────────────────────────
    // Kickstart
    // ─────────────────────────────────────────────────────────────────

    public async Task<KickstartResponse> RunKickstart(KickstartRequest request, ILlmProvider llmProvider)
    {
        var archetype = GetTemplate("archetypes", request.ArchetypeId)
            ?? throw new ArgumentException($"Archetype '{request.ArchetypeId}' not found");
        var stack = GetTemplate("stacks", request.StackId)
            ?? throw new ArgumentException($"Stack '{request.StackId}' not found");

        var promptPath = Path.Combine(_paths.GroundPrompts, "kickstart.md");
        if (!File.Exists(promptPath))
            throw new InvalidOperationException("Kickstart prompt not found");

        var promptTemplate = await File.ReadAllTextAsync(promptPath);
        var prompt = BuildKickstartPrompt(request, archetype.Content, stack.Content, promptTemplate);

        var llmOptions = new LlmOptions(_project.ProjectPath, TimeoutMs: 180000);
        var response = await llmProvider.Execute(prompt, llmOptions);

        if (!response.Success)
            throw new InvalidOperationException($"LLM execution failed: {response.Error}");

        var sections = ParseKickstartOutput(response.Output);
        var created = await WriteGroundFiles(sections, request.Name, request.ArchetypeId, request.StackId);

        return new KickstartResponse(created);
    }

    private static string BuildKickstartPrompt(KickstartRequest request, string archetypeContent, string stackContent, string promptTemplate)
    {
        var sb = new StringBuilder();

        sb.AppendLine(promptTemplate);
        sb.AppendLine();
        sb.AppendLine("## Context");
        sb.AppendLine();
        sb.AppendLine($"Product name: {request.Name}");
        if (!string.IsNullOrEmpty(request.Description))
            sb.AppendLine($"Description: {request.Description}");
        sb.AppendLine();

        sb.AppendLine("### Archetype");
        sb.AppendLine("```");
        sb.AppendLine(archetypeContent);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Stack");
        sb.AppendLine("```");
        sb.AppendLine(stackContent);
        sb.AppendLine("```");

        if (request.ArchetypeOverride != null)
        {
            sb.AppendLine();
            sb.AppendLine("### Archetype Overrides");
            var ao = request.ArchetypeOverride;
            if (!string.IsNullOrEmpty(ao.Type)) sb.AppendLine($"- type: {ao.Type}");
            if (!string.IsNullOrEmpty(ao.PrimaryUser)) sb.AppendLine($"- primary user: {ao.PrimaryUser}");
            if (!string.IsNullOrEmpty(ao.CoreLoop)) sb.AppendLine($"- core loop: {ao.CoreLoop}");
            if (!string.IsNullOrEmpty(ao.SuccessMetric)) sb.AppendLine($"- success metric: {ao.SuccessMetric}");
            if (!string.IsNullOrEmpty(ao.Monetization)) sb.AppendLine($"- monetization: {ao.Monetization}");
        }

        if (request.StackOverride != null)
        {
            sb.AppendLine();
            sb.AppendLine("### Stack Overrides");
            var so = request.StackOverride;
            if (!string.IsNullOrEmpty(so.Frontend)) sb.AppendLine($"- frontend: {so.Frontend}");
            if (!string.IsNullOrEmpty(so.Backend)) sb.AppendLine($"- backend: {so.Backend}");
            if (!string.IsNullOrEmpty(so.DataStorage)) sb.AppendLine($"- data/storage: {so.DataStorage}");
            if (!string.IsNullOrEmpty(so.InfraDeploy)) sb.AppendLine($"- infra/deploy: {so.InfraDeploy}");
            if (!string.IsNullOrEmpty(so.Testing)) sb.AppendLine($"- testing: {so.Testing}");
            if (!string.IsNullOrEmpty(so.Observability)) sb.AppendLine($"- observability: {so.Observability}");
            if (!string.IsNullOrEmpty(so.Avoid)) sb.AppendLine($"- avoid: {so.Avoid}");
        }

        return sb.ToString();
    }

    private static Dictionary<string, string> ParseKickstartOutput(string output)
    {
        var sections = new Dictionary<string, string>();
        string? currentSection = null;
        var currentContent = new StringBuilder();
        var inCodeBlock = false;

        foreach (var line in output.Split('\n'))
        {
            if (line.Trim().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                if (currentSection != null)
                    currentContent.AppendLine(line);
                continue;
            }

            if (!inCodeBlock && line.StartsWith("### "))
            {
                if (currentSection != null)
                    sections[currentSection] = ExtractCodeBlockContent(currentContent.ToString());

                currentSection = line[4..].Trim().ToLowerInvariant().Replace(" ", "-");
                currentContent.Clear();
            }
            else if (currentSection != null)
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentSection != null)
            sections[currentSection] = ExtractCodeBlockContent(currentContent.ToString());

        return sections;
    }

    private static string ExtractCodeBlockContent(string content)
    {
        var lines = content.Split('\n');
        var result = new StringBuilder();
        var inBlock = false;

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("```"))
            {
                inBlock = !inBlock;
                continue;
            }
            if (inBlock)
                result.AppendLine(line);
        }

        return result.ToString().Trim();
    }

    private async Task<List<CreatedFile>> WriteGroundFiles(Dictionary<string, string> sections, string productName, string archetypeId, string stackId)
    {
        var groundPath = Path.Combine(_project.MillFolder, "ground");
        var created = new List<CreatedFile>();

        var fileMap = new Dictionary<string, (string category, string filename)>
        {
            ["product"] = ("", "product.md"),
            ["primary-user"] = ("personas", "primary-user.md"),
            ["tech-stack"] = ("standards", "tech-stack.md"),
            ["quality-bars"] = ("standards", "quality-bars.md")
        };

        foreach (var (section, content) in sections)
        {
            if (!fileMap.TryGetValue(section, out var target)) continue;

            var (category, filename) = target;
            var filePath = string.IsNullOrEmpty(category)
                ? Path.Combine(groundPath, filename)
                : Path.Combine(groundPath, category, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, content);

            created.Add(new CreatedFile(
                category.Length > 0 ? category : "root",
                Path.GetFileNameWithoutExtension(filename),
                filePath
            ));

            _logger.LogInformation("Created ground file: {Path}", filePath);
        }

        var kickstartMeta = new { createdAt = DateTime.UtcNow, productName, archetypeId, stackId };
        var kickstartPath = Path.Combine(groundPath, "kickstart.json");
        await File.WriteAllTextAsync(kickstartPath, JsonSerializer.Serialize(kickstartMeta, JsonOptions));

        return created;
    }
}

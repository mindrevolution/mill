using MillApi.Models;

namespace MillApi.Services;

/// <summary>
/// Service for shape workspace drafts.
/// </summary>
public class DraftService
{
    private readonly ProjectContext _project;
    private readonly MarkdownService _markdown;

    public DraftService(ProjectContext project, MarkdownService markdown)
    {
        _project = project;
        _markdown = markdown;
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

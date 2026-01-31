namespace MillApi.Services;

/// <summary>
/// Shared helpers for parsing markdown files with frontmatter.
/// </summary>
public static class MarkdownHelpers
{
    /// <summary>
    /// Convert slug to display name (e.g., "my-item" -> "My Item")
    /// </summary>
    public static string FormatName(string slug)
    {
        return string.Join(" ", slug.Split('-').Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));
    }

    /// <summary>
    /// Convert text to URL-safe slug.
    /// </summary>
    public static string Slugify(string text)
    {
        return text.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("--", "-")
            .Trim('-');
    }

    /// <summary>
    /// Extract description from markdown (first paragraph after frontmatter).
    /// </summary>
    public static string ExtractDescription(string content)
    {
        var lines = content.Split('\n');
        var inFrontmatter = false;
        var description = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                inFrontmatter = !inFrontmatter;
                continue;
            }
            if (inFrontmatter)
                continue;
            if (string.IsNullOrWhiteSpace(line) && description.Count > 0)
                break;
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                description.Add(line.Trim());
        }

        return string.Join(" ", description).Trim();
    }

    /// <summary>
    /// Extract body content (everything after frontmatter).
    /// </summary>
    public static string ExtractBody(string content)
    {
        if (!content.StartsWith("---"))
            return content.Trim();

        var endMarker = content.IndexOf("---", 3);
        if (endMarker < 0)
            return content.Trim();

        return content[(endMarker + 3)..].Trim();
    }

    /// <summary>
    /// Extract body with line tracking (preserves structure).
    /// </summary>
    public static string ExtractBodyContent(string content)
    {
        var lines = content.Split('\n');
        var inFrontmatter = false;
        var pastFrontmatter = false;
        var bodyLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter)
                {
                    pastFrontmatter = true;
                    inFrontmatter = false;
                    continue;
                }
                inFrontmatter = true;
                continue;
            }
            if (pastFrontmatter)
                bodyLines.Add(line);
        }

        return string.Join('\n', bodyLines).Trim();
    }
}

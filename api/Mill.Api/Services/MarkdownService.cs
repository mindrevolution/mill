using Markdig;

namespace Mill.Api.Services;

/// <summary>
/// Renders markdown to HTML using GitHub Flavored Markdown extensions.
/// </summary>
public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // tables, task lists, autolinks, strikethrough, etc.
            .Build();
    }

    /// <summary>
    /// Render markdown to HTML.
    /// </summary>
    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return "";

        return Markdown.ToHtml(markdown, _pipeline);
    }
}

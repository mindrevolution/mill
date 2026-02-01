using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MillApi.Services;

/// <summary>
/// HTTP endpoints for PTY terminal sessions.
/// Uses SSE for output streaming, HTTP for control operations.
/// </summary>
public static class PtyEndpoints
{
    public static void MapPtyEndpoints(this WebApplication app, PtyManager ptyManager)
    {
        // High-level spec session endpoint - backend handles all command building
        app.MapPost("/api/pty/spec-session", async (HttpContext ctx, IssueService issueService, ProjectContext projectContext) =>
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var root = doc.RootElement;

                var mode = root.GetProperty("mode").GetString()!; // "draft" or "refine"
                var issueNumber = root.TryGetProperty("issueNumber", out var issueEl) ? issueEl.GetInt32() : (int?)null;
                var draftId = root.TryGetProperty("draftId", out var draftEl) ? draftEl.GetString() : null;
                var initialPrompt = root.TryGetProperty("initialPrompt", out var promptEl) ? promptEl.GetString() : null;
                var cols = root.TryGetProperty("cols", out var colsEl) ? colsEl.GetInt32() : 80;
                var rows = root.TryGetProperty("rows", out var rowsEl) ? rowsEl.GetInt32() : 24;

                // Build the prompt content
                var promptFile = mode == "refine" ? "shape/prompts/spec-refine.md" : "shape/prompts/spec-draft.md";
                var fullPromptPath = Path.Combine(Directory.GetCurrentDirectory(), promptFile);

                if (!File.Exists(fullPromptPath))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = $"Prompt file not found: {promptFile}" }));
                    return;
                }

                var promptContent = await File.ReadAllTextAsync(fullPromptPath);
                var appendContent = new StringBuilder();

                // For refine mode with issue, inject the full issue content
                if (mode == "refine" && issueNumber.HasValue)
                {
                    var issue = await issueService.Get(issueNumber.Value);
                    if (issue != null)
                    {
                        // Get GitHub URL
                        var repoUrl = await GetGitHubRepoUrl(projectContext.ProjectPath);
                        var issueUrl = repoUrl != null ? $"{repoUrl}/issues/{issueNumber}" : $"Issue #{issueNumber}";

                        appendContent.AppendLine();
                        appendContent.AppendLine("---");
                        appendContent.AppendLine();
                        appendContent.AppendLine($"# Issue #{issue.Number}: {issue.Title}");
                        appendContent.AppendLine();
                        appendContent.AppendLine($"**URL:** {issueUrl}");
                        appendContent.AppendLine($"**Type:** {issue.Type}");
                        appendContent.AppendLine($"**Status:** {issue.Status}");
                        if (issue.Labels.Count > 0)
                            appendContent.AppendLine($"**Labels:** {string.Join(", ", issue.Labels)}");
                        appendContent.AppendLine();
                        appendContent.AppendLine("## Spec Content");
                        appendContent.AppendLine();
                        appendContent.AppendLine(issue.Body);
                    }
                }
                else if (draftId != null)
                {
                    appendContent.AppendLine();
                    appendContent.AppendLine($"Resume draft: {draftId}");
                }

                // Combine prompt with appended content
                var fullPrompt = promptContent + appendContent.ToString();

                // Write to temp file
                var promptTempFile = Path.Combine(Path.GetTempPath(), $"mill-prompt-{Guid.NewGuid():N}.md");
                await File.WriteAllTextAsync(promptTempFile, fullPrompt);

                // Build command args - only include user message if provided
                string sessionId;
                if (OperatingSystem.IsWindows())
                {
                    var psCommand = string.IsNullOrEmpty(initialPrompt)
                        ? $"& claude --system-prompt (Get-Content -Raw '{promptTempFile}')"
                        : $"& claude --system-prompt (Get-Content -Raw '{promptTempFile}') -- '{initialPrompt.Replace("'", "''")}'";
                    sessionId = await ptyManager.StartSession("powershell", new[] { "-NoProfile", "-Command", psCommand }, projectContext.ProjectPath, cols, rows, promptTempFile);
                }
                else
                {
                    var shellCommand = string.IsNullOrEmpty(initialPrompt)
                        ? $"claude --system-prompt \"$(cat '{promptTempFile}')\""
                        : $"claude --system-prompt \"$(cat '{promptTempFile}')\" -- '{initialPrompt.Replace("'", "'\\''")}'";
                    sessionId = await ptyManager.StartSession("/bin/sh", new[] { "-c", shellCommand }, projectContext.ProjectPath, cols, rows, promptTempFile);
                }

                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { sessionId }));
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        });
        // Start a new PTY session
        app.MapPost("/api/pty/start", async (HttpContext ctx) =>
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                var root = doc.RootElement;

                var command = root.GetProperty("command").GetString()!;
                var argsList = root.TryGetProperty("args", out var argsEl)
                    ? argsEl.EnumerateArray().Select(a => a.GetString()!).ToList()
                    : new List<string>();
                var cwd = root.TryGetProperty("cwd", out var cwdEl) ? cwdEl.GetString() : null;
                var cols = root.TryGetProperty("cols", out var colsEl) ? colsEl.GetInt32() : 80;
                var rows = root.TryGetProperty("rows", out var rowsEl) ? rowsEl.GetInt32() : 24;

                // Handle prompt file loading
                // Write prompt to temp file, then use shell to read it (avoids escaping and length limits)
                string? promptTempFile = null;
                var finalArgs = new List<string>();

                for (int i = 0; i < argsList.Count; i++)
                {
                    if (argsList[i] == "--system-prompt-file" && i + 1 < argsList.Count)
                    {
                        var promptPath = argsList[i + 1];
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), promptPath);
                        if (File.Exists(fullPath))
                        {
                            // Copy prompt to temp file (will be cleaned up when session ends)
                            promptTempFile = Path.Combine(Path.GetTempPath(), $"mill-prompt-{Guid.NewGuid():N}.md");
                            File.Copy(fullPath, promptTempFile);
                        }
                        else
                        {
                            Console.WriteLine($"[PTY] Warning: prompt file not found: {fullPath}");
                        }
                        i++; // Skip the path argument
                    }
                    else
                    {
                        finalArgs.Add(argsList[i]);
                    }
                }

                string sessionId;
                if (promptTempFile != null)
                {
                    // Use shell to read prompt file and pass to command
                    // This avoids escaping issues and command line length limits
                    if (OperatingSystem.IsWindows())
                    {
                        // PowerShell: use Get-Content -Raw to read file
                        var escapedArgs = string.Join(" ", finalArgs.Select(a => $"'{a.Replace("'", "''")}'"));
                        var psCommand = $"& {command} --system-prompt (Get-Content -Raw '{promptTempFile}') {escapedArgs}";
                        sessionId = await ptyManager.StartSession("powershell", new[] { "-NoProfile", "-Command", psCommand }, cwd, cols, rows, promptTempFile);
                    }
                    else
                    {
                        // Unix: use sh with $(cat ...) substitution
                        var escapedArgs = string.Join(" ", finalArgs.Select(a => $"'{a.Replace("'", "'\\''")}'"));
                        var shellCommand = $"{command} --system-prompt \"$(cat '{promptTempFile}')\" {escapedArgs}";
                        sessionId = await ptyManager.StartSession("/bin/sh", new[] { "-c", shellCommand }, cwd, cols, rows, promptTempFile);
                    }
                }
                else
                {
                    sessionId = await ptyManager.StartSession(command, finalArgs.ToArray(), cwd, cols, rows, null);
                }

                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { sessionId }));
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        });

        // SSE stream for PTY output
        app.MapGet("/api/pty/{sessionId}/stream", async (string sessionId, HttpContext ctx) =>
        {
            var channel = ptyManager.GetOutputChannel(sessionId);
            if (channel == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Session not found" }));
                return;
            }

            ctx.Response.Headers.Append("Content-Type", "text/event-stream");
            ctx.Response.Headers.Append("Cache-Control", "no-cache");
            ctx.Response.Headers.Append("Connection", "keep-alive");
            ctx.Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

            var ct = ctx.RequestAborted;

            try
            {
                await foreach (var evt in channel.ReadAllAsync(ct))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        type = evt.Type,
                        data = evt.Data,
                        exitCode = evt.ExitCode,
                        error = evt.Error
                    });

                    await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected, no logging needed
            }
        });

        // Send input to PTY
        app.MapPost("/api/pty/{sessionId}/input", async (string sessionId, HttpContext ctx) =>
        {
            if (!ptyManager.SessionExists(sessionId))
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Session not found" }));
                return;
            }

            using var reader = new StreamReader(ctx.Request.Body);
            var data = await reader.ReadToEndAsync();
            ptyManager.SendInput(sessionId, data);

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { ok = true }));
        });

        // Resize PTY
        app.MapPost("/api/pty/{sessionId}/resize", async (string sessionId, HttpContext ctx) =>
        {
            if (!ptyManager.SessionExists(sessionId))
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Session not found" }));
                return;
            }

            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            var root = doc.RootElement;
            var cols = root.GetProperty("cols").GetInt32();
            var rows = root.GetProperty("rows").GetInt32();

            ptyManager.Resize(sessionId, cols, rows);

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { ok = true }));
        });

        // Kill PTY session
        app.MapDelete("/api/pty/{sessionId}", async (string sessionId, HttpContext ctx) =>
        {
            ptyManager.Kill(sessionId);

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { ok = true }));
        });
    }

    /// <summary>
    /// Get GitHub repo URL from git remote.
    /// Returns null if not a GitHub repo or git command fails.
    /// </summary>
    private static async Task<string?> GetGitHubRepoUrl(string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "remote get-url origin",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) return null;

            var remoteUrl = output.Trim();

            // Convert SSH URL to HTTPS URL if needed
            // git@github.com:owner/repo.git -> https://github.com/owner/repo
            if (remoteUrl.StartsWith("git@github.com:"))
            {
                var path = remoteUrl["git@github.com:".Length..].TrimEnd(".git".ToCharArray());
                return $"https://github.com/{path}";
            }

            // https://github.com/owner/repo.git -> https://github.com/owner/repo
            if (remoteUrl.StartsWith("https://github.com/"))
            {
                return remoteUrl.TrimEnd(".git".ToCharArray());
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

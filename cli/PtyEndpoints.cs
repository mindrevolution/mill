using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// HTTP endpoints for PTY terminal sessions.
/// Uses SSE for output streaming, HTTP for control operations.
/// </summary>
public static class PtyEndpoints
{
    public static void MapPtyEndpoints(this WebApplication app, PtyManager ptyManager)
    {
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
                            Console.WriteLine($"[PTY] Prompt file: {promptPath} -> {promptTempFile}");
                        }
                        else
                        {
                            Console.WriteLine($"[PTY] Prompt file not found: {fullPath}");
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
            Console.WriteLine($"[SSE] Stream requested for session: {sessionId}");

            var channel = ptyManager.GetOutputChannel(sessionId);
            if (channel == null)
            {
                Console.WriteLine($"[SSE] Session not found: {sessionId}");
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Session not found" }));
                return;
            }

            Console.WriteLine($"[SSE] Starting stream for session: {sessionId}");

            ctx.Response.Headers.Append("Content-Type", "text/event-stream");
            ctx.Response.Headers.Append("Cache-Control", "no-cache");
            ctx.Response.Headers.Append("Connection", "keep-alive");
            ctx.Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

            var ct = ctx.RequestAborted;
            var eventCount = 0;

            try
            {
                await foreach (var evt in channel.ReadAllAsync(ct))
                {
                    eventCount++;
                    Console.WriteLine($"[SSE] Event {eventCount} for {sessionId}: type={evt.Type}, dataLen={evt.Data?.Length ?? 0}");

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
                Console.WriteLine($"[SSE] Stream ended for {sessionId}, sent {eventCount} events");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[SSE] Client disconnected from {sessionId} after {eventCount} events");
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
}

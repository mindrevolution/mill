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
                var args = root.TryGetProperty("args", out var argsEl)
                    ? argsEl.EnumerateArray().Select(a => a.GetString()!).ToArray()
                    : Array.Empty<string>();
                var cwd = root.TryGetProperty("cwd", out var cwdEl) ? cwdEl.GetString() : null;
                var cols = root.TryGetProperty("cols", out var colsEl) ? colsEl.GetInt32() : 80;
                var rows = root.TryGetProperty("rows", out var rowsEl) ? rowsEl.GetInt32() : 24;

                var sessionId = await ptyManager.StartSession(command, args, cwd, cols, rows);

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
                // Client disconnected
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

using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Photino.NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MillApi;

/// <summary>
/// Photino workbench window with embedded web server.
/// Must run on STA thread (Windows) - see Program.cs for thread setup.
/// </summary>
static class Workbench
{
    public static int Run()
    {
        var port = FindFreePort();
        var serverUrl = $"http://127.0.0.1:{port}";
        var wwwroot = FindWwwRoot();

        WebApplication? app = null;

        try
        {
            Out.Step("starting server...");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                WebRootPath = wwwroot
            });
            builder.WebHost.UseUrls(serverUrl);
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.Services.AddCors();
            builder.Services.AddMillApi();

            app = builder.Build();

            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            if (Directory.Exists(wwwroot))
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
                app.MapFallbackToFile("index.html");
            }
            else
            {
                Out.Warn("wwwroot not found");
            }

            // Health endpoint
            app.MapGet("/api/health", () =>
                Results.Json(new HealthResponse("ok", Mill.Version), WorkbenchJsonContext.Default.HealthResponse));

            app.MapMillApi();

            try
            {
                app.StartAsync().Wait();
                Out.Ok("server ready");
            }
            catch (Exception ex)
            {
                Out.Error($"server failed: {ex.Message}");
                return 1;
            }

            var window = new PhotinoWindow()
                .SetTitle("mill workbench")
                .SetSize(1400, 900)
                .SetResizable(true)
                .Center()
                .Load(serverUrl);

            window.WaitForClose();
            return 0;
        }
        finally
        {
            app?.StopAsync().Wait();
        }
    }

    static string FindWwwRoot()
    {
        // Try published location first
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        var wwwroot = Path.Combine(exeDir, "wwwroot");

        if (!Directory.Exists(wwwroot))
        {
            // Try repo location during development
            var repoRoot = FindRepoRoot();
            if (repoRoot != null)
            {
                wwwroot = Path.Combine(repoRoot, "workbench", "dist");
            }
        }

        return wwwroot;
    }

    static string? FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

// AOT-compatible types for workbench API
record HealthResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("version")] string Version);

[JsonSerializable(typeof(HealthResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
partial class WorkbenchJsonContext : JsonSerializerContext { }

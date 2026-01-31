using System.Net;
using System.Net.Sockets;
using System.Text.Json;
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
    const int DefaultWidth = 1400;
    const int DefaultHeight = 900;
    const int MinWidth = 800;
    const int MinHeight = 600;
    const string BackgroundColor = "#09090b";

    // Set WebView2 background color before any Photino code runs - prevents white flash
    static Workbench()
    {
        // Windows: WebView2 environment variable (AARRGGBB format)
        Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "FF09090B");
    }

    /// <summary>
    /// Run API server only (no Photino window) for development.
    /// Uses ASPNETCORE_URLS env var or defaults to port 5218.
    /// Logs all requests to console for debugging.
    /// </summary>
    public static int RunApiOnly()
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5218";
        var wwwroot = FindWwwRoot();

        Out.Step("starting api server...");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            WebRootPath = wwwroot
        });
        builder.WebHost.UseUrls(urls);
        // Enable request logging for dev
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
        builder.Logging.AddFilter("MillApi", LogLevel.Information);
        builder.Logging.AddSimpleConsole(options =>
        {
            options.TimestampFormat = "HH:mm:ss.fff ";
            options.SingleLine = true;
        });
        builder.Services.AddCors();
        builder.Services.AddMillApi();

        var app = builder.Build();

        // Log all API requests
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var method = context.Request.Method;
                var path = context.Request.Path;

                // Log request start for non-OPTIONS
                if (method != "OPTIONS")
                    Console.WriteLine($"  ... {method} {path}");

                var start = DateTime.UtcNow;
                try
                {
                    await next();
                    var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                    var status = context.Response.StatusCode;
                    var color = status < 400 ? "\u001b[32m" : "\u001b[31m"; // green or red
                    var reset = "\u001b[0m";
                    Console.WriteLine($"  {color}{status}{reset} {method} {path} ({elapsed:F0}ms)");
                }
                catch (Exception ex)
                {
                    var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                    Console.WriteLine($"  \u001b[31m500\u001b[0m {method} {path} ({elapsed:F0}ms)");
                    Console.WriteLine($"      \u001b[31m{ex.GetType().Name}: {ex.Message}\u001b[0m");
                    throw;
                }
            }
            else
            {
                await next();
            }
        });

        app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        if (Directory.Exists(wwwroot))
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapFallbackToFile("index.html");
        }

        app.MapGet("/api/health", () =>
            Results.Json(new HealthResponse("ok", Mill.Version), WorkbenchJsonContext.Default.HealthResponse));

        app.MapMillApi();

        Out.Ok($"api server ready at {urls}");
        Out.Detail("press Ctrl+C to stop");
        Out.Blank();

        app.Run();
        return 0;
    }

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
                .SetTitle(GetWindowTitle())
                .SetResizable(true)
                // Start minimized to hide white flash during WebView2 init
                .SetMinimized(true)
                // Set dark background for browser control (platform-specific)
                .SetBrowserControlInitParameters(GetBrowserInitParams());

            // Set icon if available
            var iconPath = GetIconPath();
            if (iconPath != null)
                window.SetIconFile(iconPath);

            // Restore or initialize window position/size
            var state = LoadWindowState();
            if (state != null && IsValidWindowState(state))
            {
                window.SetLeft(state.Left)
                      .SetTop(state.Top)
                      .SetSize(state.Width, state.Height);
            }
            else
            {
                window.SetSize(DefaultWidth, DefaultHeight).Center();
            }

            // Save window state on close
            window.WindowClosing += (sender, e) =>
            {
                SaveWindowState(new WindowState(
                    window.Left,
                    window.Top,
                    window.Width,
                    window.Height
                ));
                return false; // allow close
            };

            // Show window when content signals it's ready
            window.RegisterWebMessageReceivedHandler((sender, msg) =>
            {
                if (msg == "ready")
                {
                    window.SetMinimized(false);
                    // Bring to front: briefly set topmost, then turn off
                    window.SetTopMost(true);
                    window.SetTopMost(false);
                }
            });

            // Load dark splash page that signals when app is loaded
            window.LoadRawString(GetSplashHtml(serverUrl));
            window.WaitForClose();
            return 0;
        }
        finally
        {
            app?.StopAsync().Wait();
        }
    }

    static string GetWindowTitle()
    {
        var repoName = GetRepoName();
        return repoName != null ? $"Mill · {repoName}" : "Mill";
    }

    static string GetBrowserInitParams()
    {
        if (OperatingSystem.IsWindows())
        {
            // WebView2: Chromium command-line arguments
            return "--background-color=09090b";
        }
        else if (OperatingSystem.IsLinux())
        {
            // WebKitGTK: JSON config for webkit settings
            // Note: background color is set via webkit_web_view_set_background_color in native code
            return "{}";
        }
        else if (OperatingSystem.IsMacOS())
        {
            // WKWebView: JSON config
            return "{}";
        }
        return "";
    }

    static string GetSplashHtml(string serverUrl) => $$"""
        <!DOCTYPE html>
        <html style="background:#09090b">
        <head>
            <meta charset="utf-8">
            <style>
                * { margin: 0; padding: 0; }
                html, body {
                    background: #09090b;
                    height: 100%;
                    width: 100%;
                    overflow: hidden;
                }
                iframe {
                    width: 100%;
                    height: 100%;
                    border: none;
                    background: #09090b;
                }
            </style>
        </head>
        <body>
            <iframe id="app" src="{{serverUrl}}"></iframe>
            <script>
                const iframe = document.getElementById('app');
                iframe.onload = () => {
                    // Signal to .NET that content is ready
                    window.external.sendMessage('ready');
                    // Replace current page with iframe content for proper navigation
                    setTimeout(() => window.location.replace('{{serverUrl}}'), 50);
                };
            </script>
        </body>
        </html>
        """;

    static string? GetRepoName()
    {
        try
        {
            var gitRoot = FindGitRoot(Directory.GetCurrentDirectory());
            if (gitRoot == null) return null;
            return Path.GetFileName(gitRoot);
        }
        catch
        {
            return null;
        }
    }

    static string? FindGitRoot(string? dir)
    {
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    static string? GetIconPath()
    {
        // Try published location first
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        var iconPath = Path.Combine(exeDir, "mill.ico");
        if (File.Exists(iconPath)) return iconPath;

        // Try repo location during development
        var repoRoot = FindRepoRoot();
        if (repoRoot != null)
        {
            iconPath = Path.Combine(repoRoot, "cli", "mill.ico");
            if (File.Exists(iconPath)) return iconPath;
        }

        return null;
    }

    static string GetWindowStatePath()
    {
        // Store in .mill folder if it exists, otherwise use user's local app data
        var millDir = Path.Combine(Directory.GetCurrentDirectory(), ".mill");
        if (Directory.Exists(millDir))
            return Path.Combine(millDir, "workbench.json");

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var millAppData = Path.Combine(appData, "mill");
        Directory.CreateDirectory(millAppData);
        return Path.Combine(millAppData, "workbench.json");
    }

    static WindowState? LoadWindowState()
    {
        try
        {
            var path = GetWindowStatePath();
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, WorkbenchJsonContext.Default.WindowState);
        }
        catch
        {
            return null;
        }
    }

    static void SaveWindowState(WindowState state)
    {
        try
        {
            var path = GetWindowStatePath();
            var json = JsonSerializer.Serialize(state, WorkbenchJsonContext.Default.WindowState);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    static bool IsValidWindowState(WindowState state)
    {
        // Basic sanity checks - window should be reasonably sized and positioned
        if (state.Width < MinWidth || state.Height < MinHeight)
            return false;
        if (state.Width > 10000 || state.Height > 10000)
            return false;
        if (state.Left < -state.Width || state.Top < -state.Height)
            return false;
        if (state.Left > 10000 || state.Top > 10000)
            return false;
        return true;
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

record WindowState(
    [property: JsonPropertyName("left")] int Left,
    [property: JsonPropertyName("top")] int Top,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(WindowState))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
partial class WorkbenchJsonContext : JsonSerializerContext { }

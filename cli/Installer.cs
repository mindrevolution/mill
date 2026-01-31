using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Install and update logic for mill.
/// </summary>
static class Installer
{
    const string RepoOwner = "mindrevolution";
    const string RepoName = "mill";
    static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "mill" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    /// <summary>
    /// System binary path: ~/.local/bin/mill or %LOCALAPPDATA%\Programs\mill\mill.exe
    /// </summary>
    public static string GetBinaryPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Programs", "mill", "mill.exe");
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "bin", "mill");
    }

    /// <summary>
    /// System data path for prompts: ~/.local/share/mill or %LOCALAPPDATA%\mill
    /// </summary>
    public static string GetDataPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "mill");
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "mill");
    }

    /// <summary>
    /// Check if running from system install location.
    /// </summary>
    public static bool IsInstalledLocation()
    {
        var exePath = Environment.ProcessPath!;
        var systemPath = GetBinaryPath();
        return string.Equals(
            Path.GetFullPath(exePath),
            Path.GetFullPath(systemPath),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>
    /// Get version of installed binary (if any).
    /// </summary>
    public static string? GetInstalledVersion()
    {
        var systemPath = GetBinaryPath();
        if (!File.Exists(systemPath)) return null;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = systemPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var parts = output.Trim().Split(' ');
            return parts.Length >= 2 ? parts[1] : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Check install status: mode, versions, update availability.
    /// </summary>
    public static async Task<InstallCheck> Check()
    {
        var current = Mill.Version;
        var isInstalled = IsInstalledLocation();
        var mode = isInstalled ? InstallMode.Update : InstallMode.Install;
        var installedVersion = isInstalled ? current : GetInstalledVersion();

        var isDowngrade = mode == InstallMode.Install &&
            installedVersion != null &&
            CompareVersions(current, installedVersion) < 0;

        // For update mode, check GitHub for latest
        string? latest = null;
        string? error = null;
        var updateAvailable = false;

        if (mode == InstallMode.Update)
        {
            try
            {
                var release = await FetchLatestRelease();
                if (release == null)
                    error = "no releases available";
                else
                {
                    latest = release.TagName.TrimStart('v');
                    updateAvailable = CompareVersions(current, latest) < 0;
                }
            }
            catch (HttpRequestException ex)
            {
                error = ex.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "rate limited — try again later"
                    : $"connection failed: {ex.Message}";
            }
            catch (Exception ex) { error = ex.Message; }
        }

        return new InstallCheck(mode, current, latest, installedVersion, updateAvailable, isDowngrade, error);
    }

    /// <summary>
    /// Copy binary and prompts to system locations.
    /// </summary>
    public static (bool Success, string Message) CopyToSystem()
    {
        try
        {
            var exePath = Environment.ProcessPath!;
            var binaryPath = GetBinaryPath();
            var dataPath = GetDataPath();
            var binaryDir = Path.GetDirectoryName(binaryPath)!;

            // Find source prompts directory (relative to running binary)
            var sourceDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var sourceRoot = Directory.GetParent(sourceDir)?.FullName;
            if (sourceRoot == null || !Directory.Exists(Path.Combine(sourceRoot, "shape/prompts")))
            {
                return (false, "prompts not found — run from repo directory");
            }

            // Copy binary and native libraries
            Out.Step("copying binary...");
            Directory.CreateDirectory(binaryDir);
            File.Copy(exePath, binaryPath, overwrite: true);

            // Copy native Photino libraries (Photino.Native.dll, WebView2Loader.dll, etc.)
            foreach (var nativeLib in Directory.GetFiles(sourceDir, "*.dll")
                .Where(f => !f.EndsWith("mill.dll", StringComparison.OrdinalIgnoreCase)))
            {
                var destFile = Path.Combine(binaryDir, Path.GetFileName(nativeLib));
                File.Copy(nativeLib, destFile, overwrite: true);
            }

            // Copy wwwroot for workbench UI
            var wwwrootSource = Path.Combine(sourceDir, "wwwroot");
            if (Directory.Exists(wwwrootSource))
            {
                Out.Step("copying wwwroot...");
                var wwwrootDest = Path.Combine(binaryDir, "wwwroot");
                if (Directory.Exists(wwwrootDest))
                    Directory.Delete(wwwrootDest, recursive: true);
                CopyDirectory(wwwrootSource, wwwrootDest);
            }

            if (!OperatingSystem.IsWindows())
            {
                var chmod = Process.Start("chmod", ["+x", binaryPath]);
                chmod?.WaitForExit();

                // Copy native libs for Linux/macOS (.so, .dylib)
                foreach (var nativeLib in Directory.GetFiles(sourceDir, "*.so")
                    .Concat(Directory.GetFiles(sourceDir, "*.dylib")))
                {
                    var destFile = Path.Combine(binaryDir, Path.GetFileName(nativeLib));
                    File.Copy(nativeLib, destFile, overwrite: true);
                }
            }

            // Copy prompts and templates by workspace
            Out.Step("copying prompts...");
            CopyDirectory(Path.Combine(sourceRoot, "ground/prompts"), Path.Combine(dataPath, "ground/prompts"));
            CopyDirectory(Path.Combine(sourceRoot, "ground/templates"), Path.Combine(dataPath, "ground/templates"));
            CopyDirectory(Path.Combine(sourceRoot, "shape/prompts"), Path.Combine(dataPath, "shape/prompts"));
            CopyDirectory(Path.Combine(sourceRoot, "shape/templates"), Path.Combine(dataPath, "shape/templates"));
            CopyDirectory(Path.Combine(sourceRoot, "ship/prompts"), Path.Combine(dataPath, "ship/prompts"));

            return (true, $"installed to {binaryPath}");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "permission denied");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }

    /// <summary>
    /// Download and install latest version from GitHub.
    /// </summary>
    public static async Task<(bool Success, string Message)> Update()
    {
        try
        {
            var release = await FetchLatestRelease();
            if (release == null)
                return (false, "no releases available");

            var assetName = Mill.AssetName;
            var asset = release.Assets.FirstOrDefault(a => a.Name == assetName);
            if (asset == null)
                return (false, $"no binary for {assetName}");

            var exePath = Environment.ProcessPath!;
            var exeDir = Path.GetDirectoryName(exePath)!;
            var tmpPath = Path.Combine(exeDir, assetName + ".tmp");

            Out.Step($"downloading {assetName}...");

            using (var response = await Http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpPath);
                await response.Content.CopyToAsync(fs);
            }

            var downloadedSize = new FileInfo(tmpPath).Length;
            if (downloadedSize == 0 || downloadedSize != asset.Size)
            {
                File.Delete(tmpPath);
                return (false, "download corrupted — size mismatch");
            }

            var oldPath = exePath + ".old";
            try { File.Delete(oldPath); } catch { }

            File.Move(exePath, oldPath);
            File.Move(tmpPath, exePath);

            if (!OperatingSystem.IsWindows())
            {
                var chmod = Process.Start("chmod", ["+x", exePath]);
                chmod?.WaitForExit();
            }

            var latest = release.TagName.TrimStart('v');
            return (true, $"updated to {latest}");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "permission denied");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Check if system binary directory is in PATH.
    /// </summary>
    public static bool IsInPath()
    {
        var binaryDir = Path.GetDirectoryName(GetBinaryPath())!;
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        return pathEnv.Split(separator).Any(dir =>
            string.Equals(Path.GetFullPath(dir), Path.GetFullPath(binaryDir),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }

    /// <summary>
    /// Get shell-specific PATH instructions.
    /// </summary>
    public static string GetPathInstructions()
    {
        var binaryDir = Path.GetDirectoryName(GetBinaryPath())!;
        if (OperatingSystem.IsWindows())
            return $"setx PATH \"%PATH%;{binaryDir}\"";
        if (OperatingSystem.IsMacOS())
            return $"echo 'export PATH=\"{binaryDir}:$PATH\"' >> ~/.zshrc";
        return $"echo 'export PATH=\"{binaryDir}:$PATH\"' >> ~/.bashrc";
    }

    /// <summary>
    /// Clean up old binary from previous update.
    /// </summary>
    public static void CleanupOldBinary()
    {
        try
        {
            var exePath = Environment.ProcessPath!;
            var oldPath = exePath + ".old";
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }
        catch { }
    }

    static async Task<GhRelease?> FetchLatestRelease()
    {
        var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        try
        {
            var json = await Http.GetStringAsync(url);
            return JsonSerializer.Deserialize<GhRelease>(json, Mill.JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    static int CompareVersions(string a, string b)
    {
        var aParts = a.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        var bParts = b.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        for (var i = 0; i < Math.Max(aParts.Length, bParts.Length); i++)
        {
            var av = i < aParts.Length ? aParts[i] : 0;
            var bv = i < bParts.Length ? bParts[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }
}

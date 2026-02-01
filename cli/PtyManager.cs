using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Pty.Net;

/// <summary>
/// Manages PTY sessions for interactive Claude Code CLI sessions.
/// Supports both Photino IPC and HTTP/SSE for cross-platform compatibility.
/// </summary>
public sealed class PtyManager : IDisposable
{
    private readonly ConcurrentDictionary<string, PtySession> _sessions = new();

    /// <summary>
    /// Start a new PTY session.
    /// </summary>
    public async Task<string> StartSession(string command, string[] args, string? cwd, int cols, int rows, string? tempFileToCleanup = null)
    {
        var sessionId = $"pty_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}".Substring(0, 32);
        cwd ??= Directory.GetCurrentDirectory();

        // Startup logging reduced to single line
        Console.WriteLine($"[PTY] Starting {sessionId}: {command} {string.Join(" ", args)} ({cols}x{rows})");

        // Build environment with current env vars
        var env = new Dictionary<string, string>();
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                env[key] = value;
            }
        }

        // Ensure TERM is set for proper terminal support
        env["TERM"] = "xterm-256color";

        var options = new PtyOptions
        {
            Name = "xterm-256color",
            Cols = cols,
            Rows = rows,
            Cwd = cwd,
            App = command,
            CommandLine = args,
            Environment = env
        };

        var pty = await PtyProvider.SpawnAsync(options, CancellationToken.None);

        var session = new PtySession(sessionId, pty, tempFileToCleanup);

        if (!_sessions.TryAdd(sessionId, session))
        {
            session.Dispose();
            throw new InvalidOperationException("Session ID collision");
        }

        // Start reading output
        session.StartReading(() =>
        {
            _sessions.TryRemove(sessionId, out _);
        });

        return sessionId;
    }

    /// <summary>
    /// Send input to a PTY session.
    /// </summary>
    public void SendInput(string sessionId, string data)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Write(data);
        }
    }

    /// <summary>
    /// Resize a PTY session.
    /// </summary>
    public void Resize(string sessionId, int cols, int rows)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Resize(cols, rows);
        }
    }

    /// <summary>
    /// Kill a PTY session.
    /// </summary>
    public void Kill(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
        }
    }

    /// <summary>
    /// Get the output channel for SSE streaming.
    /// </summary>
    public ChannelReader<PtyEvent>? GetOutputChannel(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.OutputChannel;
        }
        return null;
    }

    /// <summary>
    /// Check if a session exists.
    /// </summary>
    public bool SessionExists(string sessionId) => _sessions.ContainsKey(sessionId);

    /// <summary>
    /// Handle incoming message from React via Photino IPC.
    /// Still supported for input since JS→.NET works fine.
    /// </summary>
    public void HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "pty_input":
                    var inputSessionId = root.GetProperty("sessionId").GetString()!;
                    var data = root.GetProperty("data").GetString()!;
                    SendInput(inputSessionId, data);
                    break;
                case "pty_resize":
                    var resizeSessionId = root.GetProperty("sessionId").GetString()!;
                    var cols = root.GetProperty("cols").GetInt32();
                    var rows = root.GetProperty("rows").GetInt32();
                    Resize(resizeSessionId, cols, rows);
                    break;
                case "pty_kill":
                    var killSessionId = root.GetProperty("sessionId").GetString()!;
                    Kill(killSessionId);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PTY] Error handling message: {ex.Message}");
        }
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();
    }
}

/// <summary>
/// Event types for PTY output streaming.
/// </summary>
public record PtyEvent(string Type, string? Data = null, int? ExitCode = null, string? Error = null);

/// <summary>
/// Represents a single PTY session with channel-based output.
/// </summary>
internal sealed class PtySession : IDisposable
{
    private readonly string _sessionId;
    private readonly IPtyConnection _pty;
    private readonly Channel<PtyEvent> _outputChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly string? _tempFileToCleanup;
    private bool _disposed;

    public int ExitCode { get; private set; }
    public ChannelReader<PtyEvent> OutputChannel => _outputChannel.Reader;

    public PtySession(string sessionId, IPtyConnection pty, string? tempFileToCleanup = null)
    {
        _sessionId = sessionId;
        _pty = pty;
        _tempFileToCleanup = tempFileToCleanup;
        // Unbounded channel to avoid blocking PTY reads
        _outputChannel = Channel.CreateUnbounded<PtyEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });
    }

    public void Write(string data)
    {
        if (_disposed) return;

        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        _pty.WriterStream.Write(bytes, 0, bytes.Length);
        _pty.WriterStream.Flush();
    }

    public void Resize(int cols, int rows)
    {
        if (_disposed) return;
        _pty.Resize(cols, rows);
    }

    public void StartReading(Action onExit)
    {
        Task.Run(async () =>
        {
            var buffer = new byte[4096];
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var read = await _pty.ReaderStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (read == 0) break;

                    var data = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    await _outputChannel.Writer.WriteAsync(new PtyEvent("output", Data: data), _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no logging needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PTY] Read error for {_sessionId}: {ex.Message}");
            }
            finally
            {
                ExitCode = _pty.ExitCode;
                Console.WriteLine($"[PTY] Session {_sessionId} exited with code {ExitCode}");
                _outputChannel.Writer.TryWrite(new PtyEvent("exit", ExitCode: ExitCode));
                _outputChannel.Writer.Complete();
                onExit();
            }
        }, _cts.Token);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _outputChannel.Writer.TryComplete();

        try
        {
            _pty.Dispose();
        }
        catch
        {
            // Ignore dispose errors
        }

        // Clean up temp prompt file if one was created
        if (_tempFileToCleanup != null)
        {
            try { File.Delete(_tempFileToCleanup); } catch { }
        }

        _cts.Dispose();
    }
}

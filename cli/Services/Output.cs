namespace Mill.Services;

/// <summary>
/// Central output formatting for human-readable CLI output.
/// </summary>
public static class Output
{
    private static readonly bool IsTerminal = !Console.IsOutputRedirected;
    private static readonly bool SupportsAnsi = IsTerminal && !OperatingSystem.IsWindows() ||
        (Environment.GetEnvironmentVariable("WT_SESSION") != null) ||
        (Environment.GetEnvironmentVariable("TERM_PROGRAM") != null);

    // ANSI codes
    private static readonly string Reset = SupportsAnsi ? "\x1b[0m" : "";
    private static readonly string Dim = SupportsAnsi ? "\x1b[2m" : "";
    private static readonly string Bold = SupportsAnsi ? "\x1b[1m" : "";
    private static readonly string Yellow = SupportsAnsi ? "\x1b[33m" : "";
    private static readonly string Green = SupportsAnsi ? "\x1b[32m" : "";
    private static readonly string Cyan = SupportsAnsi ? "\x1b[36m" : "";
    private static readonly string Red = SupportsAnsi ? "\x1b[31m" : "";

    /// <summary>
    /// Print a section header.
    /// </summary>
    public static void Header(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"{Bold}{text}{Reset}");
        Console.WriteLine($"{Dim}{"".PadRight(Math.Min(text.Length, 40), '─')}{Reset}");
    }

    /// <summary>
    /// Print an empty state message.
    /// </summary>
    public static void Empty(string message)
    {
        Console.WriteLine($"{Dim}{message}{Reset}");
    }

    /// <summary>
    /// Print a success message.
    /// </summary>
    public static void Success(string message)
    {
        Console.WriteLine($"{Green}✓{Reset} {message}");
    }

    /// <summary>
    /// Print an info message.
    /// </summary>
    public static void Info(string message)
    {
        Console.WriteLine($"{Cyan}→{Reset} {message}");
    }

    /// <summary>
    /// Print a warning message.
    /// </summary>
    public static void Warn(string message)
    {
        Console.WriteLine($"{Yellow}!{Reset} {message}");
    }

    /// <summary>
    /// Print a list item with tag and title.
    /// </summary>
    public static void ListItem(string tag, string title, string? detail = null)
    {
        Console.WriteLine($"  {Dim}[{Reset}{tag}{Dim}]{Reset} {title}");
        if (!string.IsNullOrEmpty(detail))
        {
            Console.WriteLine($"       {Dim}{Truncate(detail, 70)}{Reset}");
        }
    }

    /// <summary>
    /// Print a list item with number prefix.
    /// </summary>
    public static void NumberedItem(int number, string tag, string title)
    {
        Console.WriteLine($"  {Dim}#{number}{Reset} {Dim}[{Reset}{tag}{Dim}]{Reset} {title}");
    }

    /// <summary>
    /// Print a key-value field.
    /// </summary>
    public static void Field(string name, string? value, bool dim = false)
    {
        if (value == null) return;
        var val = dim ? $"{Dim}{value}{Reset}" : value;
        Console.WriteLine($"  {Dim}{name}:{Reset} {val}");
    }

    /// <summary>
    /// Print a date field.
    /// </summary>
    public static void DateField(string name, DateTime date)
    {
        Field(name, date.ToString("yyyy-MM-dd HH:mm"));
    }

    /// <summary>
    /// Print a horizontal rule.
    /// </summary>
    public static void Rule()
    {
        Console.WriteLine($"{Dim}{"".PadRight(40, '─')}{Reset}");
    }

    /// <summary>
    /// Print a blank line.
    /// </summary>
    public static void Blank()
    {
        Console.WriteLine();
    }

    /// <summary>
    /// Print a title line.
    /// </summary>
    public static void Title(string text)
    {
        Console.WriteLine($"{Bold}{text}{Reset}");
    }

    /// <summary>
    /// Print a history entry.
    /// </summary>
    public static void HistoryItem(DateTime date, bool success, int issue, string title, string? prUrl = null)
    {
        var icon = success ? $"{Green}✓{Reset}" : $"{Red}✗{Reset}";
        Console.WriteLine($"  {Dim}{date:yyyy-MM-dd HH:mm}{Reset}  {icon}  {Dim}#{issue}{Reset} {title}");
        if (!string.IsNullOrEmpty(prUrl))
        {
            Console.WriteLine($"                        {Dim}→ {prUrl}{Reset}");
        }
    }

    /// <summary>
    /// Print a simple bullet item.
    /// </summary>
    public static void Bullet(string text)
    {
        Console.WriteLine($"  • {text}");
    }

    /// <summary>
    /// Print body content (e.g., markdown).
    /// </summary>
    public static void Body(string content)
    {
        Console.WriteLine();
        Console.WriteLine(content);
    }

    /// <summary>
    /// Print a URL.
    /// </summary>
    public static void Url(string url)
    {
        Console.WriteLine($"  {Dim}→{Reset} {Cyan}{url}{Reset}");
    }

    /// <summary>
    /// Print a score with visual indicator.
    /// </summary>
    public static void Score(int score, int max, string verdict)
    {
        var color = score >= 7 ? Green : score >= 4 ? Yellow : Red;
        var bar = new string('█', score) + $"{Dim}{new string('░', max - score)}{Reset}";
        Console.WriteLine($"  {bar} {color}{score}/{max}{Reset} {Dim}({verdict}){Reset}");
    }

    /// <summary>
    /// Print a ship iteration progress line.
    /// </summary>
    public static void ShipIteration(int iteration, int max, string signal, string? detail, long durationMs)
    {
        var icon = signal switch
        {
            "MILL_CONTINUE" => $"{Cyan}→{Reset}",
            "MILL_VERIFY" => $"{Yellow}◆{Reset}",
            "MILL_ABORT" => $"{Red}✗{Reset}",
            "MILL_DONE" => $"{Green}✓{Reset}",
            "MILL_REJECTED" => $"{Red}↻{Reset}",
            _ => $"{Dim}?{Reset}"
        };
        var time = durationMs >= 1000 ? $"{durationMs / 1000}s" : $"{durationMs}ms";
        Console.WriteLine($"  {icon}  {Dim}[{iteration}/{max}]{Reset} {signal} {Dim}({time}){Reset}");
        if (!string.IsNullOrEmpty(detail))
        {
            Console.WriteLine($"       {Dim}{Truncate(detail, 70)}{Reset}");
        }
    }

    /// <summary>
    /// Print ship run summary.
    /// </summary>
    public static void ShipSummary(bool success, int iterations, long durationMs, string? prUrl)
    {
        Blank();
        Rule();
        if (success)
        {
            var time = durationMs >= 1000 ? $"{durationMs / 1000}s" : $"{durationMs}ms";
            Success($"Ship complete — {iterations} iteration(s) in {time}");
            if (!string.IsNullOrEmpty(prUrl))
            {
                Url(prUrl);
            }
        }
        else
        {
            Warn($"Ship failed after {iterations} iteration(s)");
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 1)] + "…";
    }
}

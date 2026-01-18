#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

var exit = Run(args);
Environment.Exit(exit);

int Run(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        PrintHelp();
        return 0;
    }

    var command = args[0].Trim().ToLowerInvariant();
    var rest = args.Skip(1).ToArray();
    var millHome = ResolveMillHome();
    var promptsDir = Path.Combine(millHome, "prompts");

    return command switch
    {
        "warmup" => RunPromptCommand(promptsDir, "warmup", rest),
        "question" => RunPromptCommand(promptsDir, "question", rest),
        "concept" => RunPromptCommand(promptsDir, "concept", rest),
        "feature" => RunPromptCommand(promptsDir, "feature", rest, requiresContext: true),
        "model" => RunPromptCommand(promptsDir, "model", rest, requiresContext: true),
        "plan" => RunPromptCommand(promptsDir, "plan", rest, requiresContext: true),
        "architect" => RunPromptCommand(promptsDir, "architect", rest),
        "create-issue" => RunPromptCommand(promptsDir, "create-issue", rest, requiresContext: true),
        "intake" => RunIntake(promptsDir, rest),
        "classify" => RunClassify(promptsDir, rest),
        "issue" => RunPromptCommand(promptsDir, "issue", rest, requiresContext: true),
        "mill-iteration" => RunPromptCommand(promptsDir, "mill-iteration", rest, requiresContext: true),
        "mill-loop" => RunMillLoop(promptsDir, rest),
        "promote" => RunPromote(promptsDir, rest),
        _ => Fail($"Unknown command: {command}")
    };
}

int RunPromptCommand(string promptsDir, string promptName, string[] rest, bool requiresContext = false)
{
    if (requiresContext)
    {
        EnsureContext(promptsDir);
    }

    var promptPath = Path.Combine(promptsDir, $"{promptName}.md");
    if (!File.Exists(promptPath))
    {
        return Fail($"Prompt not found: {promptPath}");
    }

    var userPrompt = string.Join(" ", rest).Trim();
    return RunPrompt(promptPath, userPrompt);
}

int RunIntake(string promptsDir, string[] rest)
{
    var input = string.Join(" ", rest).Trim();
    if (string.IsNullOrWhiteSpace(input))
    {
        return Fail("Intake requires input text.");
    }

    var intakeDir = Path.Combine(Directory.GetCurrentDirectory(), "spec", "intake");
    Directory.CreateDirectory(intakeDir);
    var slug = Slugify(input, 6);
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    var intakePath = Path.Combine(intakeDir, $"{timestamp}-{slug}.md");

    var content = $"# Intake\n\n{input}\n";
    File.WriteAllText(intakePath, content, Encoding.UTF8);
    Console.WriteLine($"Saved intake: {intakePath}");

    var promptPath = Path.Combine(promptsDir, "classify.md");
    if (!File.Exists(promptPath))
    {
        return 0;
    }

    var classifyPrompt = $"INTAKE_PATH: {intakePath}";
    return RunPrompt(promptPath, classifyPrompt);
}

int RunClassify(string promptsDir, string[] rest)
{
    if (rest.Length == 0)
    {
        return Fail("Classify requires a path to an intake file.");
    }

    var promptPath = Path.Combine(promptsDir, "classify.md");
    if (!File.Exists(promptPath))
    {
        return Fail($"Prompt not found: {promptPath}");
    }

    var intakePath = rest[0];
    var userPrompt = $"INTAKE_PATH: {intakePath}";
    return RunPrompt(promptPath, userPrompt);
}

int RunMillLoop(string promptsDir, string[] rest)
{
    if (rest.Length == 0)
    {
        return Fail("mill-loop requires a plan path.");
    }

    EnsureContext(promptsDir);

    var planPath = rest[0];
    var maxIterations = rest.Length > 1 && int.TryParse(rest[1], out var mi) ? mi : 20;
    var completionPromise = rest.Length > 2 ? rest[2] : "MILL_DONE";
    var sleepSeconds = rest.Length > 3 && int.TryParse(rest[3], out var ss) ? ss : 0;

    var loopDir = Path.Combine(Directory.GetCurrentDirectory(), ".mill");
    Directory.CreateDirectory(loopDir);
    var logPath = Path.Combine(loopDir, $"mill-loop-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");

    var promptPath = Path.Combine(promptsDir, "mill-iteration.md");
    if (!File.Exists(promptPath))
    {
        return Fail($"Prompt not found: {promptPath}");
    }

    for (var i = 1; i <= maxIterations; i++)
    {
        var userPrompt = $"PLAN_PATH: {planPath}\nITERATION: {i}\nMAX_ITERATIONS: {maxIterations}\nCOMPLETION_PROMISE: {completionPromise}";
        var output = RunPromptCapture(promptPath, userPrompt);
        File.AppendAllText(logPath, output + Environment.NewLine);

        if (output.Contains(completionPromise, StringComparison.Ordinal))
        {
            Console.WriteLine(completionPromise);
            return 0;
        }

        if (sleepSeconds > 0)
        {
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(sleepSeconds));
        }
    }

    Console.WriteLine("NOT_DONE");
    return 1;
}

int RunPromote(string promptsDir, string[] rest)
{
    if (rest.Length == 0)
    {
        return Fail("promote requires a plan path.");
    }

    EnsureContext(promptsDir);

    var planPath = rest[0];
    if (!File.Exists(planPath))
    {
        return Fail($"Plan not found: {planPath}");
    }

    var planText = File.ReadAllText(planPath);
    if (!planText.Contains("## Loop Contract", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Plan is missing a Loop Contract section.");
    }

    if (!planText.Contains("Completion Promise", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Plan is missing a Completion Promise.");
    }

    var modelDir = Path.Combine(Directory.GetCurrentDirectory(), "spec", "model");
    if (!Directory.Exists(modelDir))
    {
        return Fail("spec/model/ is missing. Create a model before promotion.");
    }

    var promptPath = Path.Combine(promptsDir, "issue.md");
    if (!File.Exists(promptPath))
    {
        return Fail($"Prompt not found: {promptPath}");
    }

    var userPrompt = $"PLAN_PATH: {planPath}\nMODEL_DIR: {modelDir}";
    return RunPrompt(promptPath, userPrompt);
}

void EnsureContext(string promptsDir)
{
    var contextPath = Path.Combine(Directory.GetCurrentDirectory(), "spec", ".context.md");
    if (File.Exists(contextPath))
    {
        return;
    }

    var promptPath = Path.Combine(promptsDir, "warmup.md");
    if (File.Exists(promptPath))
    {
        RunPrompt(promptPath, "");
    }
}

int RunPrompt(string promptPath, string userPrompt)
{
    var output = RunPromptCapture(promptPath, userPrompt);
    Console.WriteLine(output);
    return 0;
}

string RunPromptCapture(string promptPath, string userPrompt)
{
    var template = File.ReadAllText(promptPath);
    var rendered = template.Replace("{{USER_PROMPT}}", userPrompt ?? string.Empty);

    var cli = Environment.GetEnvironmentVariable("MILL_CLI");
    if (string.IsNullOrWhiteSpace(cli))
    {
        cli = Environment.GetEnvironmentVariable("RALPH_CLI");
    }
    if (string.IsNullOrWhiteSpace(cli))
    {
        cli = "claude-code";
    }

    var psi = new ProcessStartInfo
    {
        FileName = cli,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using var process = Process.Start(psi);
    if (process == null)
    {
        throw new InvalidOperationException("Failed to start CLI process.");
    }

    process.StandardInput.Write(rendered);
    process.StandardInput.Close();

    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();

    return string.IsNullOrWhiteSpace(stderr) ? stdout : stdout + Environment.NewLine + stderr;
}

string ResolveMillHome()
{
    var env = Environment.GetEnvironmentVariable("MILL_HOME");
    if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
    {
        return env;
    }

    var cwd = Directory.GetCurrentDirectory();
    if (Directory.Exists(Path.Combine(cwd, "prompts")))
    {
        return cwd;
    }

    throw new InvalidOperationException("MILL_HOME is not set and prompts/ was not found in the current directory.");
}

string Slugify(string input, int wordLimit)
{
    var words = Regex.Matches(input.ToLowerInvariant(), "[a-z0-9]+")
        .Select(m => m.Value)
        .Take(wordLimit)
        .ToArray();

    return words.Length == 0 ? "item" : string.Join("-", words);
}

int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

void PrintHelp()
{
    Console.WriteLine("MILL CLI");
    Console.WriteLine("Usage: dotnet run app.cs <command> [args]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  intake <text>            Save raw input and run classify");
    Console.WriteLine("  classify <path>          Classify an intake file");
    Console.WriteLine("  concept <text>");
    Console.WriteLine("  feature <text>");
    Console.WriteLine("  model <text>");
    Console.WriteLine("  plan <text>");
    Console.WriteLine("  issue <text>");
    Console.WriteLine("  create-issue <text>");
    Console.WriteLine("  warmup");
    Console.WriteLine("  question <text>");
    Console.WriteLine("  architect <text>");
    Console.WriteLine("  mill-iteration <text>");
    Console.WriteLine("  mill-loop <plan> [max] [promise] [sleep]");
    Console.WriteLine("  promote <plan>");
}"})}*** End Patch**Note: input format is JSON with exact schema matching the tool. If you need to pass a string, wrap it in JSON with a "command" field. Because this is a freeform tool, I should not wrap JSON. I mistakenly used apply_patch with JSON. Need to re-run correctly.** Let's call apply_patch properly.** 

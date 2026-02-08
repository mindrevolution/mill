using System.Reflection;
using System.Text;
using Mill.Commands;

Console.OutputEncoding = Encoding.UTF8;

// Parse global flags
var human = args.Contains("--human") || args.Contains("-h");
args = args.Where(a => a != "--human" && a != "-h").ToArray();

// Version
var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion?.Split('+')[0] ?? "unknown";

// Route commands
return args switch
{
    [] => ShowHelp(),
    ["--version"] or ["-v"] => ShowVersion(),
    ["help"] or ["--help"] => ShowHelp(),

    ["init", ..] => InitCommand.Run(human),

    ["ground", .. var rest] => await GroundCommand.Run(rest, human),
    ["idea", .. var rest] => await IdeaCommand.Run(rest, human),
    ["draft", .. var rest] => await DraftCommand.Run(rest, human),
    ["spec", .. var rest] => await SpecCommand.Run(rest, human),
    ["history", .. var rest] => await HistoryCommand.Run(rest, human),
    ["context", .. var rest] => await ContextCommand.Run(rest, human),
    ["template", .. var rest] => await TemplateCommand.Run(rest, human),

    _ => ShowHelp()
};

int ShowVersion()
{
    Console.WriteLine($"mill {version}");
    return 0;
}

int ShowHelp()
{
    Console.WriteLine($"""
        mill {version} - specification-first delivery system

        usage: mill <command> [options] [--human]

        commands:
          init                    initialize .mill/ in current repo

          ground list [category]  list knowledge items
          ground get <cat> <id>   get knowledge item content
          ground create <cat> <id> <content>  create knowledge item

          idea list               list active ideas
          idea get <id>           get idea details
          idea create <title> <intent>  create a new idea
          idea drop <id> <essence>  drop an idea with learned essence
          idea dropped            list dropped ideas

          draft list              list spec drafts
          draft get <slug>        get draft content
          draft validate <slug>   get validation result
          draft publish <slug>    publish draft to GitHub issue

          spec list               list open specs
          spec get <number>       get spec details

          history                 list run history
          history add <json>      add a history entry

          context                 show context.md
          context status          show context freshness

          template list <type>    list templates (archetypes, stacks, specs)
          template get <type> <id>  get template content

        flags:
          --human, -h             human-readable output (default: JSON)
          --version, -v           show version

        Use with Claude Code skills:
          /mill:ground            knowledge management
          /mill:idea              idea capture
          /mill:spec              spec drafting
          /mill:ship              bounded work loops
          /mill:warmup            generate context.md
          /mill:question          answer questions
        """);
    return 0;
}

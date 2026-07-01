using System.Text;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class FlaggedWordAddCommand : IConsoleCommand
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "flagword:add";
    public string Description => "Adds a flagged word.";
    public string Help => "flagword:add <word> [severity=Low|Medium|High] [partial=true|false]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 3)
        {
            shell.WriteError(Help);
            return;
        }

        var word = args[0].Trim();

        if (string.IsNullOrWhiteSpace(word))
        {
            shell.WriteError("Word cannot be empty.");
            return;
        }

        var severity = FlaggedWordSeverity.Low;
        if (args.Length >= 2 &&
            !Enum.TryParse(args[1], ignoreCase: true, out severity))
        {
            shell.WriteError($"Invalid severity \"{args[1]}\". Valid values: {string.Join(", ", Enum.GetNames<FlaggedWordSeverity>())}");
            return;
        }

        var matchPartials = false;
        if (args.Length >= 3 &&
            !bool.TryParse(args[2], out matchPartials))
        {
            shell.WriteError($"Invalid partial-match value \"{args[2]}\". Use true or false.");
            return;
        }

        await _db.AddFlaggedWordAsync(word, severity, matchPartials);

        ReloadFlaggedWords();

        shell.WriteLine($"Added flagged word \"{word}\" with severity {severity} and partial matching set to {matchPartials}.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<word>"),
            2 => CompletionResult.FromHintOptions(Enum.GetNames<FlaggedWordSeverity>(), "severity"),
            3 => CompletionResult.FromHintOptions(["true", "false"], "match partials"),
            _ => CompletionResult.Empty,
        };
    }

    private void ReloadFlaggedWords()
    {
        var chat = _systems.GetEntitySystem<ChatSystem>();
        chat.ReloadFlaggedWords();
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class FlaggedWordRemoveCommand : IConsoleCommand
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "flagword:remove";
    public string Description => "Removes a flagged word.";
    public string Help => "flagword:remove <word>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var word = args[0].Trim();

        if (string.IsNullOrWhiteSpace(word))
        {
            shell.WriteError("Word cannot be empty.");
            return;
        }

        await _db.RemoveFlaggedWordAsync(word);

        ReloadFlaggedWords();

        shell.WriteLine($"Removed flagged word \"{word}\".");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint("<word>")
            : CompletionResult.Empty;
    }

    private void ReloadFlaggedWords()
    {
        var chat = _systems.GetEntitySystem<ChatSystem>();
        chat.ReloadFlaggedWords();
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed partial class FlaggedWordViewCommand : IConsoleCommand
{
    [Dependency] private IServerDbManager _db = default!;

    public string Command => "flagword:view";
    public string Description => "Views flagged words.";
    public string Help => "flagword:view";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Help);
            return;
        }

        var words = await _db.GetFlaggedWordsAsync();

        if (words.Count == 0)
        {
            shell.WriteLine("No flagged words are configured.");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Flagged words:");

        foreach (var word in words)
        {
            builder.Append("- ");
            builder.Append(word.Word);
            builder.Append(" | Severity: ");
            builder.Append(word.Severity);
            builder.Append(" | Partial matches: ");
            builder.Append(word.FlagPartialMatches);
            builder.Append(" | Enabled: ");
            builder.Append(word.Enabled);
            builder.AppendLine();
        }

        shell.WriteLine(builder.ToString());
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class FlaggedWordUpdateCommand : IConsoleCommand
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "flagword:update";
    public string Description => "Updates a flagged word by deleting the old word and adding a new one.";
    public string Help => "flagword:update <word> [severity=Low|Medium|High] [partial=true|false]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 3)
        {
            shell.WriteError(Help);
            return;
        }

        var word = args[0].Trim();

        if (string.IsNullOrWhiteSpace(word))
        {
            shell.WriteError("Word cannot be empty.");
            return;
        }

        var severity = FlaggedWordSeverity.High;
        if (args.Length >= 2 &&
            !Enum.TryParse(args[1], ignoreCase: true, out severity))
        {
            shell.WriteError($"Invalid severity \"{args[2]}\". Valid values: {string.Join(", ", Enum.GetNames<FlaggedWordSeverity>())}");
            return;
        }

        var matchPartials = false;
        if (args.Length >= 3 &&
            !bool.TryParse(args[2], out matchPartials))
        {
            shell.WriteError($"Invalid partial-match value \"{args[3]}\". Use true or false.");
            return;
        }

        await _db.RemoveFlaggedWordAsync(word);
        await _db.AddFlaggedWordAsync(word, severity, matchPartials);

        ReloadFlaggedWords();

        shell.WriteLine($"Updated flagged word \"{word}\" to severity {severity} and partial matching set to {matchPartials}.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<word>"),
            2 => CompletionResult.FromHintOptions(Enum.GetNames<FlaggedWordSeverity>(), "severity"),
            3 => CompletionResult.FromHintOptions(["true", "false"], "match partials"),
            _ => CompletionResult.Empty,
        };
    }

    private void ReloadFlaggedWords()
    {
        var chat = _systems.GetEntitySystem<ChatSystem>();
        chat.ReloadFlaggedWords();
    }
}

using Content.Client.NPC;
using Content.Shared.NPC;
using JetBrains.Annotations;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Client.Commands;

[UsedImplicitly]
public sealed class DebugPathfindingCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "pathfinder";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _entitySystemManager.GetEntitySystem<PathfindingSystem>();

        if (args.Length == 0)
        {
            system.Modes = PathfindingDebugMode.None;
            return;
        }

        foreach (var arg in args)
        {
            if (!Enum.TryParse<PathfindingDebugMode>(arg, out var mode))
            {
                shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error", ("arg", arg)));
                continue;
            }

            system.Modes ^= mode;
            shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify", ("arg", arg), ("newMode", (system.Modes & mode) != 0x0)));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length > 1)
        {
            return CompletionResult.Empty;
        }

        var values = Enum.GetValues<PathfindingDebugMode>().ToList();
        var options = new List<CompletionOption>();

        foreach (var val in values)
        {
            if (val == PathfindingDebugMode.None)
                continue;

            options.Add(new CompletionOption(val.ToString()));
        }

        return CompletionResult.FromOptions(options);
    }
}

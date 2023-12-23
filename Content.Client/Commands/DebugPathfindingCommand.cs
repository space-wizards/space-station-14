using Content.Client.NPC;
using Content.Shared.NPC;
using JetBrains.Annotations;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    public sealed class DebugPathfindingCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => Loc.GetString("debug-pathfinding-command-description");
        public string Help => Loc.GetString("debug-pathfinding-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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
                    shell.WriteError(Loc.GetString("debug-pathfinding-command-error", ("arg", arg)));
                    continue;
                }

                system.Modes ^= mode;
                shell.WriteLine(Loc.GetString("debug-pathfinding-command-notify", ("arg", arg), ("newMode", (system.Modes & mode) != 0x0)));
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
}

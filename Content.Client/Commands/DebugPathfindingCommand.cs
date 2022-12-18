using System.Linq;
using Content.Client.NPC;
using Content.Shared.NPC;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    public sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "pathfinder [options]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();

            if (args.Length == 0)
            {
                system.Modes = PathfindingDebugMode.None;
                return;
            }

            foreach (var arg in args)
            {
                if (!Enum.TryParse<PathfindingDebugMode>(arg, out var mode))
                {
                    shell.WriteError($"Unrecognised pathfinder args {arg}");
                    continue;
                }

                system.Modes ^= mode;
                shell.WriteLine($"Toggled {arg} to {(system.Modes & mode) != 0x0}");
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

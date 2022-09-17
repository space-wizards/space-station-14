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
        public string Help => "pathfinder [breadcrumbs]";

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
                switch (arg)
                {
                    case "breadcrumbs":
                        system.Modes ^= PathfindingDebugMode.Breadcrumbs;
                        shell.WriteLine($"Toggled {arg} to {system.Modes & PathfindingDebugMode.Breadcrumbs}");
                        break;
                    default:
                        shell.WriteError($"Unrecognised pathfinder args {arg}");
                        break;
                }
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length > 1)
            {
                return CompletionResult.Empty;
            }

            var options = new CompletionOption[]
            {
                new("breadcrumbs")
            };

            return CompletionResult.FromOptions(options);
        }
    }
}

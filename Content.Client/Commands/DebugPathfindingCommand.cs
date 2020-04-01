using Content.Client.GameObjects.Components.Pathfinding;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
#if DEBUG
            var anyAction = false;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "disable":
                        ClientPathfindingDebugComponent.DisableAll();
                        anyAction = true;
                        break;
                    case "paths":
                        ClientPathfindingDebugComponent.ToggleTooltip(PathfindingDebugMode.Route);
                        anyAction = true;
                        break;
                    case "graph":
                        ClientPathfindingDebugComponent.ToggleTooltip(PathfindingDebugMode.Graph);
                        anyAction = true;
                        break;
                    default:
                        continue;
                }
            }

            return !anyAction;
#endif
            return true;
        }
    }
}

using Content.Client.GameObjects.EntitySystems.AI;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "pathfinder [hide/nodes/routes/graph/regioncache/regions]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
#if DEBUG
            if (args.Length < 1)
            {
                shell.RemoteExecuteCommand(argStr);
                return;
            }

            var anyAction = false;
            var debugSystem = EntitySystem.Get<ClientPathfindingDebugSystem>();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "hide":
                        debugSystem.Disable();
                        anyAction = true;
                        break;
                    // Shows all nodes on the closed list
                    case "nodes":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Nodes);
                        anyAction = true;
                        break;
                    // Will show just the constructed route
                    case "routes":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Route);
                        anyAction = true;
                        break;
                    // Shows all of the pathfinding chunks
                    case "graph":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Graph);
                        anyAction = true;
                        break;
                    // Shows every time the cached reachable regions are hit (whether cached already or not)
                    case "regioncache":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.CachedRegions);
                        anyAction = true;
                        break;
                    // Shows all of the regions in each chunk
                    case "regions":
                        debugSystem.ToggleTooltip(PathfindingDebugMode.Regions);
                        anyAction = true;
                        break;
                    
                    default:
                        continue;
                }
            }

            if(!anyAction)
                shell.RemoteExecuteCommand(argStr);
#else
            shell.RemoteExecuteCommand(argStr);
#endif
        }
    }
}

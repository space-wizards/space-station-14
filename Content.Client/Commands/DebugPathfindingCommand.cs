using Content.Client.GameObjects.Components.AI;
using Content.Client.GameObjects.Components.Pathfinding;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class DebugPathfindingCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "pathfinder";
        public string Description => "Toggles visibility of pathfinding debuggers.";
        public string Help => "pathfinder [disable/nodes/routes/graph]";

        public bool Execute(IDebugConsole console, params string[] args)
        {
#if DEBUG
            if (args.Length < 1)
            {
                return true;
            }

            var anyAction = false;
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var playerEntity = playerManager.LocalPlayer.ControlledEntity;
            ClientPathfindingDebugComponent debug;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "disable":
                        if (playerEntity.HasComponent<ClientPathfindingDebugComponent>())
                        {
                            playerEntity.RemoveComponent<ClientPathfindingDebugComponent>();
                        }
                        anyAction = true;
                        break;
                    // Shows all nodes on the closed list
                    case "nodes":
                        debug = AddPathfindingDebug(playerEntity);
                        debug.ToggleTooltip(PathfindingDebugMode.Nodes);
                        anyAction = true;
                        break;
                    // Will show just the constructed route
                    case "routes":
                        debug = AddPathfindingDebug(playerEntity);
                        debug.ToggleTooltip(PathfindingDebugMode.Route);
                        anyAction = true;
                        break;
                    // Shows all of the pathfinding chunks
                    case "graph":
                        debug = AddPathfindingDebug(playerEntity);
                        debug.ToggleTooltip(PathfindingDebugMode.Graph);
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

        private ClientPathfindingDebugComponent AddPathfindingDebug(IEntity entity)
        {
            if (entity.TryGetComponent(out ClientPathfindingDebugComponent debugComponent))
            {
                return debugComponent;
            }

            return entity.AddComponent<ClientPathfindingDebugComponent>();
        }
    }
}

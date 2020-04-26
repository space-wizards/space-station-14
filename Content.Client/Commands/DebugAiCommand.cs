using Content.Client.GameObjects.Components.AI;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    /// <summary>
    /// This is used to handle the tooltips above AI mobs
    /// </summary>
    [UsedImplicitly]
    internal sealed class DebugAiCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "debugai";
        public string Description => "Handles all tooltip debugging above AI mobs";
        public string Help => "debugai [disable/paths/thonk]";

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
            MobTooltipDebugComponent tooltip;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "disable":
                        if (playerEntity.HasComponent<MobTooltipDebugComponent>())
                        {
                            playerEntity.RemoveComponent<MobTooltipDebugComponent>();
                        }
                        anyAction = true;
                        break;
                    // This will show the pathfinding numbers above the mob's head
                    case "paths":
                        tooltip = AddTooltip(playerEntity);
                        tooltip.ToggleTooltip(MobTooltips.Paths);
                        anyAction = true;
                        break;
                    // Shows stats on what the AI was thinking.
                    case "thonk":
                        tooltip = AddTooltip(playerEntity);
                        tooltip.ToggleTooltip(MobTooltips.Thonk);
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

        private MobTooltipDebugComponent AddTooltip(IEntity entity)
        {
            if (entity.TryGetComponent(out MobTooltipDebugComponent debugComponent))
            {
                return debugComponent;
            }

            return entity.AddComponent<MobTooltipDebugComponent>();
        }
    }
}

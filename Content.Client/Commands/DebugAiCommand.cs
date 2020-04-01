using Content.Client.GameObjects.Components.AI;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;

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
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
#if DEBUG
            if (args.Length < 1)
            {
                return true;
            }

            var anyAction = false;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "disable":
                        MobTooltipDebugComponent.DisableAll();
                        anyAction = true;
                        break;
                    // This will show the pathfinding numbers above the mob's head
                    case "paths":
                        MobTooltipDebugComponent.ToggleTooltip(MobTooltips.Paths);
                        anyAction = true;
                        break;
                    // Shows stats on what the AI was thinking.
                    case "thonk":
                        MobTooltipDebugComponent.ToggleTooltip(MobTooltips.Thonk);
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

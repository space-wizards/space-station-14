using Content.Client.AI;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

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
        public string Help => "debugai [hide/paths/thonk]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
#if DEBUG
            if (args.Length < 1)
            {
                shell.RemoteExecuteCommand(argStr);
                return;
            }

            var anyAction = false;
            var debugSystem = EntitySystem.Get<ClientAiDebugSystem>();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "hide":
                        debugSystem.Disable();
                        anyAction = true;
                        break;
                    // This will show the pathfinding numbers above the mob's head
                    case "paths":
                        debugSystem.ToggleTooltip(AiDebugMode.Paths);
                        anyAction = true;
                        break;
                    // Shows stats on what the AI was thinking.
                    case "thonk":
                        debugSystem.ToggleTooltip(AiDebugMode.Thonk);
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

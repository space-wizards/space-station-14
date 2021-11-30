using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public class SendNukeCodesCommand : IConsoleCommand
    {
        public string Command => "nukecodes";
        public string Description => "Send nuke codes to the communication console";
        public string Help => "nukecodes";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<NukeCodeSystem>().SendNukeCodes();
        }
    }
}

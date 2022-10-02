using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SendNukeCodesCommand : IConsoleCommand
    {
        public string Command => "nukecodes";
        public string Description => "Send nuke codes to the communication console";
        public string Help => "nukecodes [station EntityUid]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!EntityUid.TryParse(args[1], out var uid))
            {
                shell.WriteError("Could not parse station UID.");
            }

            IoCManager.Resolve<EntityManager>().System<NukeCodePaperSystem>().SendNukeCodes(uid);
        }
    }
}

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
        public string Description => "Send nuke codes to a station's communication consoles";
        public string Help => "nukecodes [station EntityUid]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError("shell-need-exactly-one-argument");
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            }

            IoCManager.Resolve<EntityManager>().System<NukeCodePaperSystem>().SendNukeCodes(uid);
        }
    }
}

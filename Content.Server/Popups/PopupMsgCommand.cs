using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Popups;
using Robust.Shared.Console;

namespace Content.Server.Popups
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class PopupMsgCommand : IConsoleCommand
    {
        public string Command => "srvpopupmsg";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var source = EntityUid.Parse(args[0]);
            var viewer = EntityUid.Parse(args[1]);
            var msg = args[2];

            source.PopupMessage(viewer, msg);
        }
    }
}

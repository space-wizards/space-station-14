using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Popups;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Popups
{
    [AdminCommand(AdminFlags.Debug)]
    public class PopupMsgCommand : IConsoleCommand
    {
        public string Command => "srvpopupmsg";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var source = EntityUid.Parse(args[0]);
            var viewer = EntityUid.Parse(args[1]);
            var msg = args[2];

            entityMgr.GetEntity(source).PopupMessage(entityMgr.GetEntity(viewer), msg);
        }
    }
}

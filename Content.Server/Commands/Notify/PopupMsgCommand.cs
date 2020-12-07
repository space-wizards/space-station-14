using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Notify
{
    [AdminCommand(AdminFlags.Debug)]
    public class PopupMsgCommand : IClientCommand
    {
        public string Command => "srvpopupmsg";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var source = EntityUid.Parse(args[0]);
            var viewer = EntityUid.Parse(args[1]);
            var msg = args[2];

            entityMgr.GetEntity(source).PopupMessage(entityMgr.GetEntity(viewer), msg);
        }
    }
}

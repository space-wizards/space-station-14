using Content.Server.Interfaces;
using Content.Shared;
using Content.Shared.Interfaces;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Utility;

namespace Content.Server
{
    public class ServerNotifyManager : SharedNotifyManager, IServerNotifyManager
    {
#pragma warning disable 649
        [Dependency] private IServerNetManager _netManager;
#pragma warning restore 649

        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotify>(nameof(MsgDoNotify));
            _initialized = true;
        }

        public override void PopupMessage(GridLocalCoordinates coordinates, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotify>();
            netMessage.Coordinates = coordinates;
            netMessage.Message = message;
            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

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
}

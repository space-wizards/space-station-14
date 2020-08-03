using Content.Server.Interfaces;
using Content.Shared;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

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

            _netManager.RegisterNetMessage<MsgDoNotifyCursor>(nameof(MsgDoNotifyCursor));
            _netManager.RegisterNetMessage<MsgDoNotifyCoordinates>(nameof(MsgDoNotifyCoordinates));
            _netManager.RegisterNetMessage<MsgDoNotifyEntity>(nameof(MsgDoNotifyEntity));
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipCursor>(nameof(MsgDoNotifyTooltipCursor));
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipCoordinates>(nameof(MsgDoNotifyTooltipCoordinates));
            _netManager.RegisterNetMessage<MsgDoNotifyTooltipEntity>(nameof(MsgDoNotifyTooltipEntity));

            _initialized = true;
        }

        public override void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyEntity>();
            netMessage.Entity = source.Uid;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyCoordinates>();
            netMessage.Coordinates = coordinates;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupMessageCursor(IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyCursor>();
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupTooltip(IEntity source, IEntity viewer, string title, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyTooltipEntity>();
            netMessage.Title = title;
            netMessage.Entity = source.Uid;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupTooltip(GridCoordinates coordinates, IEntity viewer, string title, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyTooltipCoordinates>();
            netMessage.Title = title;
            netMessage.Coordinates = coordinates;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupTooltipCursor(IEntity viewer, string title, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyTooltipCursor>();
            netMessage.Title = title;
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

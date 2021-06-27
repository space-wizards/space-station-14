using Content.Shared;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Notification.Managers
{
    public class ServerNotifyManager : SharedNotifyManager, IServerNotifyManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotifyCursor>();
            _netManager.RegisterNetMessage<MsgDoNotifyCoordinates>();
            _netManager.RegisterNetMessage<MsgDoNotifyEntity>();

            _initialized = true;
        }

        public override void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyEntity>();
            netMessage.Entity = source.Uid;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.PlayerSession.ConnectedClient);
        }

        public override void PopupMessage(EntityCoordinates coordinates, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyCoordinates>();
            netMessage.Coordinates = coordinates;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.PlayerSession.ConnectedClient);
        }

        public override void PopupMessageCursor(IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyCursor>();
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.PlayerSession.ConnectedClient);
        }
    }
}

using Content.Server.Interfaces;
using Content.Shared;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server
{
    public class ServerNotifyManager : SharedNotifyManager, IServerNotifyManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotifyCursor>(nameof(MsgDoNotifyCursor));
            _netManager.RegisterNetMessage<MsgDoNotifyCoordinates>(nameof(MsgDoNotifyCoordinates));
            _netManager.RegisterNetMessage<MsgDoNotifyEntity>(nameof(MsgDoNotifyEntity));

            _initialized = true;
        }

        public override void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyEntity>();
            netMessage.Entity = source.Uid;
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }

        public override void PopupMessage(EntityCoordinates coordinates, IEntity viewer, string message)
        {
            if (!viewer.TryGetComponent(out IActorComponent? actor))
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
            if (!viewer.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var netMessage = _netManager.CreateNetMessage<MsgDoNotifyCursor>();
            netMessage.Message = message;

            _netManager.ServerSendMessage(netMessage, actor.playerSession.ConnectedClient);
        }
    }
}

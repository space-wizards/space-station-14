using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared.Notification.Managers
{
    public abstract class SharedNotifyManager : ISharedNotifyManager
    {
        public abstract void PopupMessage(IEntity source, IEntity viewer, string message);

        public abstract void PopupMessage(EntityCoordinates coordinates, IEntity viewer, string message);

        public abstract void PopupMessageCursor(IEntity viewer, string message);

        protected class MsgDoNotifyCursor : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public string Message { get; set; } = string.Empty;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Message = buffer.ReadString();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Message);
            }
        }

        protected class MsgDoNotifyCoordinates : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public string Message { get; set; } = string.Empty;
            public EntityCoordinates Coordinates;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Message = buffer.ReadString();
                Coordinates = buffer.ReadEntityCoordinates();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Message);
                buffer.Write(Coordinates);
            }
        }

        protected class MsgDoNotifyEntity : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public string Message { get; set; } = string.Empty;
            public EntityUid Entity;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Message = buffer.ReadString();
                Entity = buffer.ReadEntityUid();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Message);
                buffer.Write(Entity);
            }
        }
    }
}

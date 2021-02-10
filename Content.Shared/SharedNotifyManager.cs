using Content.Shared.Interfaces;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared
{
    public abstract class SharedNotifyManager : ISharedNotifyManager
    {
        public abstract void PopupMessage(IEntity source, IEntity viewer, string message);

        public abstract void PopupMessage(EntityCoordinates coordinates, IEntity viewer, string message);

        public abstract void PopupMessageCursor(IEntity viewer, string message);

        protected class MsgDoNotifyCursor : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyCursor);
            public MsgDoNotifyCursor(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Message { get; set; }

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
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyCoordinates);
            public MsgDoNotifyCoordinates(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Message { get; set; }
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
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyEntity);
            public MsgDoNotifyEntity(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Message { get; set; }
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

using Content.Shared.Interfaces;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared
{
    public abstract class SharedNotifyManager : ISharedNotifyManager
    {
        public abstract void PopupMessage(IEntity source, IEntity viewer, string message);

        public abstract void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message);

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
            public GridCoordinates Coordinates;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Message = buffer.ReadString();
                Coordinates = buffer.ReadGridLocalCoordinates();
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

        public abstract void PopupTooltip(IEntity source, IEntity viewer, string title, string message);

        public abstract void PopupTooltip(GridCoordinates coordinates, IEntity viewer, string title, string message);

        public abstract void PopupTooltipCursor(IEntity viewer, string title, string message);

        protected class MsgDoNotifyTooltipCursor : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyTooltipCursor);
            public MsgDoNotifyTooltipCursor(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Title { get; set; }
            public string Message { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Title = buffer.ReadString();
                Message = buffer.ReadString();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Title);
                buffer.Write(Message);
            }
        }

        protected class MsgDoNotifyTooltipCoordinates : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyTooltipCoordinates);
            public MsgDoNotifyTooltipCoordinates(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Title { get; set; }
            public string Message { get; set; }
            public GridCoordinates Coordinates;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Title = buffer.ReadString();
                Message = buffer.ReadString();
                Coordinates = buffer.ReadGridLocalCoordinates();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Title);
                buffer.Write(Message);
                buffer.Write(Coordinates);
            }
        }

        protected class MsgDoNotifyTooltipEntity : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotifyTooltipEntity);
            public MsgDoNotifyTooltipEntity(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Title { get; set; }
            public string Message { get; set; }
            public EntityUid Entity;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Title = buffer.ReadString();
                Message = buffer.ReadString();
                Entity = buffer.ReadEntityUid();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Title);
                buffer.Write(Message);
                buffer.Write(Entity);
            }
        }
    }
}

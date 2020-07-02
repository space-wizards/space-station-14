using Content.Shared.Interfaces;
using Lidgren.Network;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared
{
    public abstract class SharedNotifyManager : ISharedNotifyManager
    {
        public void PopupMessage(IEntity source, IEntity viewer, string message)
        {
            // TODO: we might eventually want for this to pass the actual entity,
            // so the notify could track the entity movement visually.
            PopupMessage(source.Transform.GridPosition, viewer, message);
        }

        public abstract void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message);
        public abstract void PopupMessageCursor(IEntity viewer, string message);

        protected class MsgDoNotify : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgDoNotify);
            public MsgDoNotify(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string Message { get; set; }
            public bool AtCursor { get; set; }
            public GridCoordinates Coordinates { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Message = buffer.ReadString();
                AtCursor = buffer.ReadBoolean();
                if (!AtCursor)
                {
                    Coordinates = buffer.ReadGridLocalCoordinates();
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Message);
                buffer.Write(AtCursor);

                if (!AtCursor)
                {
                    buffer.Write(Coordinates);
                }
            }
        }
    }
}

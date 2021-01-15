using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    public class MsgFlash : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgFlash);
        public MsgFlash(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public int TimeMilliseconds;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            TimeMilliseconds = buffer.ReadVariableInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(TimeMilliseconds);
        }
    }
}


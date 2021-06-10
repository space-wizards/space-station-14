#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.GameWindow
{
    public sealed class MsgRequestWindowAttention : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgRequestWindowAttention);
        public MsgRequestWindowAttention(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            // Nothing
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            // Nothing
        }
    }
}

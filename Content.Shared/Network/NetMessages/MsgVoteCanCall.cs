using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    /// <summary>
    ///     Used to tell clients whether they are able to currently call votes.
    /// </summary>
    public sealed class MsgVoteCanCall : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgVoteCanCall);

        public MsgVoteCanCall(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public bool CanCall;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            CanCall = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(CanCall);
        }
    }
}

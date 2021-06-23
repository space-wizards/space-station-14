#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Voting
{
    /// <summary>
    ///     Used to tell clients whether they are able to currently call votes.
    /// </summary>
    public sealed class MsgVoteCanCall : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

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

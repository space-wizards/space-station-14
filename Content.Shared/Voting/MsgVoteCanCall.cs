using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Voting
{
    /// <summary>
    ///     Used to tell clients whether they are able to currently call votes.
    /// </summary>
    public sealed class MsgVoteCanCall : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        // If true, we can currently call votes.
        public bool CanCall;
        // When we can call votes again in server RealTime.
        // Can be null if the reason is something not timeout related.
        public TimeSpan WhenCanCallVote;

        // Which standard votes are currently unavailable, and when will they become available.
        // The whenAvailable can be null if the reason is something not timeout related.
        public (StandardVoteType type, TimeSpan whenAvailable)[] VotesUnavailable = default!;

        // It's possible to be able to call votes but all standard votes to be timed out.
        // In this case you can open the interface and see the timeout listed there, I suppose.

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            CanCall = buffer.ReadBoolean();
            buffer.ReadPadBits();
            WhenCanCallVote = TimeSpan.FromTicks(buffer.ReadInt64());

            var lenVotes = buffer.ReadByte();
            VotesUnavailable = new (StandardVoteType type, TimeSpan whenAvailable)[lenVotes];
            for (var i = 0; i < lenVotes; i++)
            {
                var type = (StandardVoteType) buffer.ReadByte();
                var timeOut = TimeSpan.FromTicks(buffer.ReadInt64());

                VotesUnavailable[i] = (type, timeOut);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(CanCall);
            buffer.WritePadBits();
            buffer.Write(WhenCanCallVote.Ticks);

            buffer.Write((byte) VotesUnavailable.Length);
            foreach (var (type, timeout) in VotesUnavailable)
            {
                buffer.Write((byte) type);
                buffer.Write(timeout.Ticks);
            }
        }
    }
}

using Robust.Server.Player;

namespace Content.Server.Voting
{
    public interface IVoteHandle
    {
        event VoteFinishedEventHandler OnFinished;
        bool IsValidOption(int optionId);
        void CastVote(IPlayerSession session, int? optionId);
    }
}

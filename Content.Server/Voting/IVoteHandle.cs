using System.Collections.Generic;
using Robust.Server.Player;

namespace Content.Server.Voting
{
    public interface IVoteHandle
    {
        int Id { get; }
        string Title { get; }
        string InitiatorText { get; }
        bool Finished { get; }
        bool Cancelled { get; }

        IReadOnlyDictionary<object, int> VotesPerOption { get; }

        event VoteFinishedEventHandler OnFinished;
        bool IsValidOption(int optionId);
        void CastVote(IPlayerSession session, int? optionId);
        void Cancel();
    }
}

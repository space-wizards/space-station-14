using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Server.Player;


namespace Content.Server.Voting.Managers
{
    public interface IVoteManager
    {
        IEnumerable<IVoteHandle> ActiveVotes { get; }
        bool TryGetVote(int voteId, [NotNullWhen(true)] out IVoteHandle? vote);

        bool CanCallVote(IPlayerSession initiator);
        void CreateRestartVote(IPlayerSession? initiator);
        void CreatePresetVote(IPlayerSession? initiator);
        IVoteHandle CreateVote(VoteOptions options);

        void Initialize();
        void Update();
    }
}

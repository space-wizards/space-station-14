using System.Diagnostics.CodeAnalysis;
using Robust.Server.Player;

#nullable enable

namespace Content.Server.Voting
{
    public interface IVoteManager
    {
        void Initialize();
        bool TryGetVote(int voteId, [NotNullWhen(true)] out IVoteHandle? vote);
        void CreateRestartVote(IPlayerSession? initiator);
        void CreatePresetVote(IPlayerSession? initiator);
        void Update();
        IVoteHandle CreateVote(VoteOptions options);
    }
}

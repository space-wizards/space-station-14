using System.Diagnostics.CodeAnalysis;
using Content.Shared.Voting;
using Robust.Shared.Player;

namespace Content.Server.Voting.Managers
{
    /// <summary>
    /// Manages in-game votes that players can vote on.
    /// </summary>
    public interface IVoteManager
    {
        /// <summary>
        /// All votes that are currently active and can be voted on by players.
        /// </summary>
        IEnumerable<IVoteHandle> ActiveVotes { get; }

        /// <summary>
        /// Try to get a vote handle by integer ID.
        /// </summary>
        /// <remarks>
        /// Only votes that are currently active can be retrieved.
        /// </remarks>
        /// <param name="voteId">The integer ID of the vote, corresponding to <see cref="IVoteHandle.Id"/>.</param>
        /// <param name="vote">The vote handle, if found.</param>
        /// <returns>True if the vote was found and it was returned, false otherwise.</returns>
        bool TryGetVote(int voteId, [NotNullWhen(true)] out IVoteHandle? vote);

        /// <summary>
        /// Check if a player can initiate a vote right now. Optionally of a specified standard type.
        /// </summary>
        /// <remarks>
        /// Players cannot start votes if they have made another vote recently,
        /// or if the specified vote type has been made recently.
        /// </remarks>
        /// <param name="initiator">The player to check.</param>
        /// <param name="voteType">
        /// The standard vote type to check cooldown for.
        /// Null to only check timeout for all vote types for the specified player.
        /// </param>
        /// <returns>
        /// True if <paramref name="initiator"/> can start votes right now,
        /// and if provided if they can start votes of type <paramref name="voteType"/>.
        /// </returns>
        bool CanCallVote(ICommonSession initiator, StandardVoteType? voteType = null);

        /// <summary>
        /// Initiate a standard vote such as restart round, that can be initiated by players.
        /// </summary>
        /// <param name="initiator">
        /// The player that called the vote.
        /// If null it is assumed to be an automatic vote by the server.
        /// </param>
        /// <param name="voteType">The type of standard vote to make.</param>
        void CreateStandardVote(ICommonSession? initiator, StandardVoteType voteType);

        /// <summary>
        /// Create a non-standard vote with special parameters.
        /// </summary>
        /// <param name="options">The options specifying the vote's behavior.</param>
        /// <returns>A handle to the created vote.</returns>
        IVoteHandle CreateVote(VoteOptions options);

        void Initialize();
        void Update();
    }
}

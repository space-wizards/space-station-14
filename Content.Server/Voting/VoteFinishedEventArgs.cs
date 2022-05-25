using System.Collections.Immutable;


namespace Content.Server.Voting
{
    public sealed class VoteFinishedEventArgs : EventArgs
    {
        /// <summary>
        ///     Null if stalemate.
        /// </summary>
        public readonly object? Winner;

        /// <summary>
        ///     Winners. More than one if there was a stalemate.
        /// </summary>
        public readonly ImmutableArray<object> Winners;

        public VoteFinishedEventArgs(object? winner, ImmutableArray<object> winners)
        {
            Winner = winner;
            Winners = winners;
        }
    }
}

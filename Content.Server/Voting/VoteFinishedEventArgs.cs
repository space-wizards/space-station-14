using System;

#nullable enable

namespace Content.Server.Voting
{
    public sealed class VoteFinishedEventArgs : EventArgs
    {
        /// <summary>
        ///     Null if stalemate.
        /// </summary>
        public readonly object? Winner;

        public VoteFinishedEventArgs(object? winner)
        {
            Winner = winner;
        }
    }
}

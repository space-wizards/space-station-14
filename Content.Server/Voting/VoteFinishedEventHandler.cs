#nullable enable

namespace Content.Server.Voting
{
    public delegate void VoteFinishedEventHandler(IVoteHandle sender, VoteFinishedEventArgs args);
}

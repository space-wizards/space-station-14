
namespace Content.Server.Voting
{
    public delegate void VoteFinishedEventHandler(IVoteHandle sender, VoteFinishedEventArgs args);
    public delegate void VoteCancelledEventHandler(IVoteHandle sender);
}

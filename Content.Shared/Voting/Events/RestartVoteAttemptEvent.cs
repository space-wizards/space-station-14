namespace Content.Shared.Voting.Events;

/// <summary>
/// Raised when a restart vote is requested.
/// Used to count the number of players who are in "dead" roles
/// </summary>
[ByRefEvent]
public struct RestartVoteAttemptEvent
{
    public int DeadPlayers = 0;

    public RestartVoteAttemptEvent(){}
}

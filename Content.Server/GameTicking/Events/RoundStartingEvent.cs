namespace Content.Server.GameTicking.Events;

/// <summary>
///     Raised at the start of <see cref="GameTicker.StartRound"/>, after round id has been incremented
/// </summary>
public sealed class RoundStartingEvent : EntityEventArgs
{
    public RoundStartingEvent(int id)
    {
        Id = id;
    }

    public int Id { get; }
}

namespace Content.Shared.Magic.Events;

[ByRefEvent]
public struct BeforeCastSpellEvent(EntityUid performer)
{
    /// <summary>
    /// The Performer of the event, to check if they meet the requirements.
    /// </summary>
    public EntityUid Performer = performer;

    public bool Cancelled;
}

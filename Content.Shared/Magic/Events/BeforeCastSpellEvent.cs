namespace Content.Shared.Magic.Events;

[ByRefEvent]
public struct BeforeCastSpellEvent
{
    /// <summary>
    ///     The Performer of the event, to check if they meet the requirements.
    /// </summary>
    public EntityUid Performer;

    public bool Cancelled;

    public BeforeCastSpellEvent(EntityUid performer)
    {
        Performer = performer;
    }
}

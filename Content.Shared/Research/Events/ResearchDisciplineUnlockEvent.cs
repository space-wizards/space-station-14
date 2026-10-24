namespace Content.Shared.Research.Events;

/// <summary>
/// Changes the number of research disciplines available to the database receiving this event.
/// </summary>
[DataDefinition]
public sealed partial class ResearchDisciplineUnlockEvent : EntityEventArgs
{
    [DataField]
    public int Amount = 1;
}

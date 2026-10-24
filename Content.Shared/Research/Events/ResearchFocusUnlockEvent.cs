namespace Content.Shared.Research.Events;

/// <summary>
/// Changes the number of research focuses available to the database receiving this event.
/// </summary>
[DataDefinition]
public sealed partial class ResearchFocusUnlockEvent : EntityEventArgs
{
    [DataField]
    public int Amount = 1;
}

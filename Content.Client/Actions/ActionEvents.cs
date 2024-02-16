namespace Content.Client.Actions;

/// <summary>
///     This event is raised when a user clicks on an empty action slot. Enables other systems to fill this slot.
/// </summary>
public sealed class FillActionSlotEvent : EntityEventArgs
{
    public EntityUid? Action;
}

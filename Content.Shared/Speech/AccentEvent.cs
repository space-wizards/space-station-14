using Content.Shared.Inventory;

namespace Content.Shared.Speech;

[ByRefEvent]
public sealed class AccentGetEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    /// <summary>
    ///     The entity to apply the accent to.
    /// </summary>
    public EntityUid Entity { get; }

    /// <summary>
    ///     The message to apply the accent transformation to.
    ///     Modify this to apply the accent.
    /// </summary>
    public string Message { get; set; }

    public AccentGetEvent(EntityUid entity, string message)
    {
        Entity = entity;
        Message = message;
    }
}

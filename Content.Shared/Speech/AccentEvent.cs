using Content.Shared.Inventory;

namespace Content.Shared.Speech;

/// <summary>
///     Raised on an entity to apply speech accents to its message.
///     Handlers should modify <see cref="Message"/> in place.
///     Relayed through inventory (e.g. voice masks) and status effects.
/// </summary>
[ByRefEvent]
public sealed class AccentGetEvent(EntityUid entity, string message) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    ///     The entity to apply the accent to.
    /// </summary>
    public EntityUid Entity { get; } = entity;

    /// <summary>
    ///     The message to apply the accent transformation to.
    ///     Modify this to apply the accent.
    /// </summary>
    public string Message { get; set; } = message;
}

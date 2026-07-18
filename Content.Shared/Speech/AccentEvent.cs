using Content.Shared.Inventory;

namespace Content.Shared.Speech;

/// <summary>
///     Raised on an entity to apply speech accents to its message.
///     Handlers should modify <see cref="Message"/> in place.
///     Relayed through inventory (e.g. voice masks) and status effects.
/// </summary>
[ByRefEvent]
public record struct AccentGetEvent(EntityUid Entity, string Message) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

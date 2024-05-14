using Content.Shared.Inventory;

namespace Content.Shared.Gravity;

/// <summary>
/// Inventory event raised to see if an entity is kept on the floor by something, like magboots.
/// Set <c>Handled</c> to prevent weightlessness on grids without gravity.
/// </summary>
[ByRefEvent]
public sealed class CheckGravityEvent : HandledEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;
}

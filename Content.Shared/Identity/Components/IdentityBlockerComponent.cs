using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Identity.Components;

[RegisterComponent, NetworkedComponent]
public sealed class IdentityBlockerComponent : Component
{
}

/// <summary>
///     Raised on an entity and relayed to inventory to determine if its identity should be knowable.
/// </summary>
public sealed class SeeIdentityAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
}

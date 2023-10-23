using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.IdentityManagement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IdentityBlockerComponent : Component
{
    public bool Enabled = true;
}

/// <summary>
///     Raised on an entity and relayed to inventory to determine if its identity should be knowable.
/// </summary>
public sealed class SeeIdentityAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    // i.e. masks or helmets.
    public SlotFlags TargetSlots => SlotFlags.MASK | SlotFlags.HEAD;
}

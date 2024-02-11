using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.IdentityManagement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IdentityBlockerComponent : Component
{
    public bool Enabled = true;

    /// <summary>
    /// What part of your face does this cover? Eyes, mouth, or full?
    /// </summary>
    [DataField]
    public IdentityBlockerCoverage Coverage = IdentityBlockerCoverage.FULL;
}

public enum IdentityBlockerCoverage
{
    NONE  = 0,
    MOUTH = 1 << 0,
    EYES  = 1 << 1,
    FULL  = MOUTH | EYES
}

/// <summary>
///     Raised on an entity and relayed to inventory to determine if its identity should be knowable.
/// </summary>
public sealed class SeeIdentityAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    // i.e. masks, helmets, or glasses.
    public SlotFlags TargetSlots => SlotFlags.MASK | SlotFlags.HEAD | SlotFlags.EYES;

    // cumulative coverage from each relayed slot
    public IdentityBlockerCoverage TotalCoverage = IdentityBlockerCoverage.NONE;
}

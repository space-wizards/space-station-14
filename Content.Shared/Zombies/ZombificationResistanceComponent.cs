using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Zombies;

/// <summary>
/// An armor-esque component for clothing that grants "resistance" (lowers the chance) against getting infected.
/// It works on a coefficient system, so 0.3 is better than 0.9, 1 is no resistance, and 0 is full resistance.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class ZombificationResistanceComponent : Component
{
    /// <summary>
    ///  The multiplier that will by applied to the zombification chance.
    /// </summary>
    [DataField]
    public float ZombificationResistanceCoefficient = 1;

    /// <summary>
    /// Examine string for the zombification resistance.
    /// Passed <c>value</c> from 0 to 100.
    /// </summary>
    [DataField]
    public LocId Examine = "zombification-resistance-coefficient-value";
}

/// <summary>
/// Gets the total resistance from the ZombificationResistanceComponent, i.e. just all of them multiplied together.
/// </summary>
public sealed class ZombificationResistanceQueryEvent : EntityEventArgs, IInventoryRelayEvent
{
    /// <summary>
    /// All slots to relay to
    /// </summary>
    public SlotFlags TargetSlots { get; }

    /// <summary>
    /// The Total of all Coefficients.
    /// </summary>
    public float TotalCoefficient = 1.0f;

    public ZombificationResistanceQueryEvent(SlotFlags slots)
    {
        TargetSlots = slots;
    }
}

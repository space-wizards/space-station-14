using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Zombies;

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
    public LocId Examine = "zombification-resistance-coefficient-value";
}

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

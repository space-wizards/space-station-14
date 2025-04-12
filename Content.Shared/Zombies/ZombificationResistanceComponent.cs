using Robust.Shared.GameStates;

namespace Content.Shared.Zombies;

[NetworkedComponent, RegisterComponent]
public sealed partial class ZombificationResistanceComponent : Component
{
    /// <summary>
    ///  The multiplier that will by applied to the cha
    /// </summary>
    [DataField("coefficient")]
    public float ZombificationResistanceCoefficient = 1;

    /// <summary>
    /// Examine string for the zombification resistance.
    /// Passed <c>value</c> from 0 to 100.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId Examine = "zombification-resistance-coefficient-value";
}

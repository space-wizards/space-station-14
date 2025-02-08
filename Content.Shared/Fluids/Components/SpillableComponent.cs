using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Makes a solution contained in this entity spillable.
/// Spills can occur when a container with this component overflows,
/// is used to melee attack something, is equipped (see <see cref="SpillWorn"/>),
/// lands after being thrown, or has the Spill verb used.
/// </summary>
[RegisterComponent]
public sealed partial class SpillableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "puddle";

    [DataField]
    public float? SpillDelay;

    /// <summary>
    ///     At most how much reagent can be splashed on someone at once?
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMeleeSpillAmount = FixedPoint2.New(20);

    /// Imp addition
    /// <summary>
    ///     Should this item be allowed to deal melee damage when spilling?
    /// </summary>
    [DataField]
    public bool AllowMeleeDamage = false;

    /// <summary>
    ///     Should this item be spilled when thrown?
    /// </summary>
    [DataField]
    public bool SpillWhenThrown = true;
}

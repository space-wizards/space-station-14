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

    /// <summary>
    ///     Should this item be spilled when worn as clothing?
    ///     Doesn't count for pockets or hands.
    /// </summary>
    [DataField]
    public bool SpillWorn = true;

    [DataField]
    public float? SpillDelay;

    /// <summary>
    ///     At most how much reagent can be splashed on someone at once?
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMeleeSpillAmount = FixedPoint2.New(20);

    /// <summary>
    ///     Should this item be spilled when thrown?
    /// </summary>
    [DataField]
    public bool SpillWhenThrown = true;
}

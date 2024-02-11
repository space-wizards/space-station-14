using Content.Shared.FixedPoint;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
public sealed partial class SpillableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "puddle";

    /// <summary>
    ///     Should this item be spilled when worn as clothing?
    ///     Doesn't count for pockets or hands.
    /// </summary>
    [DataField("spillWorn")]
    public bool SpillWorn = true;

    [DataField("spillDelay")]
    public float? SpillDelay;

    /// <summary>
    ///     At most how much reagent can be splashed on someone at once?
    /// </summary>
    [DataField("maxMeleeSpillAmount")]
    public FixedPoint2 MaxMeleeSpillAmount = FixedPoint2.New(20);
}

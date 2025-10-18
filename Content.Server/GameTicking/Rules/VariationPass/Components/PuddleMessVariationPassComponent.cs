using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
///     Handles spilling puddles with various reagents randomly around the station.
/// </summary>
[RegisterComponent]
public sealed partial class PuddleMessVariationPassComponent : Component
{
    /// <summary>
    ///     Tiles before one spill on average.
    /// </summary>
    [DataField]
    public float TilesPerSpillAverage = 600f;

    [DataField]
    public float TilesPerSpillStdDev = 50f;

    /// <summary>
    ///     Weighted random prototype to use for random messes.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomFillSolutionPrototype> RandomPuddleSolutionFill = default!;
}

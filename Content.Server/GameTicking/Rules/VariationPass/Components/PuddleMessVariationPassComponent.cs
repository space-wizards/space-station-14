using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

[RegisterComponent]
public sealed partial class PuddleMessVariationPassComponent : Component
{
    /// <summary>
    ///     Tiles before one spill on average.
    /// </summary>
    [DataField]
    public float TilesPerSpillAverage = 750f;

    [DataField]
    public float TilesPerSpillStdDev = 50f;

    /// <summary>
    ///     Weighted random prototype to use for random messes.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomFillSolutionPrototype> RandomPuddleSolutionFill = default!;
}

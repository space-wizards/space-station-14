using Content.Server.OuterRim.Worldgen.PointOfInterest;
using Robust.Shared.Prototypes;

namespace Content.Server.OuterRim.Worldgen.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("pointOfInterest")]
public sealed class PointOfInterestPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("generator", required: true)]
    public PointOfInterestGenerator Generator { get; } = default!;
}

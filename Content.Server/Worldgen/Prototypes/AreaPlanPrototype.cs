using Content.Server.Worldgen.Floorplanners;
using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("debris")]
public sealed class DebrisPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The floorplans used for this debris.
    /// </summary>
    [DataField("floorplanners", required: true)]
    public readonly List<FloorplanConfig> Floorplanners = default!;
}

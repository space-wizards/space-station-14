using Content.Server.Worldgen.Systems.Debris;
using Content.Shared.Maps;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Worldgen.Components.Debris;

/// <summary>
///     This is used for constructing asteroid debris.
/// </summary>
[RegisterComponent]
[Access(typeof(BlobFloorPlanBuilderSystem))]
public sealed partial class BlobFloorPlanBuilderComponent : Component
{
    /// <summary>
    ///     The probability that placing a floor tile will add up to three-four neighboring tiles as well.
    /// </summary>
    [DataField("blobDrawProb")] public float BlobDrawProb;

    /// <summary>
    ///     The maximum radius for the structure.
    /// </summary>
    [DataField("radius", required: true)] public float Radius;

    /// <summary>
    ///     The tiles to be used for the floor plan.
    /// </summary>
    [DataField("floorTileset", required: true,
        customTypeSerializer: typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> FloorTileset { get; private set;  } = default!;

    /// <summary>
    ///     The number of floor tiles to place when drawing the asteroid layout.
    /// </summary>
    [DataField("floorPlacements", required: true)]
    public int FloorPlacements { get; private set; }
}


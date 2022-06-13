using Content.Server.Worldgen.Systems.Floorplanners;
using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Worldgen.Floorplanners;

public sealed record AsteroidFloorplanConfig : FloorplanConfig
{
    public override Type FloorplannerSystem => typeof(AsteroidFloorplannerSystem);

    /// <summary>
    /// The number of floor tiles this will place down.
    /// </summary>
    [DataField("floorPlacements", required: true)]
    public int FloorPlacements = 0;

    /// <summary>
    /// The radius of the debris chunk.
    /// </summary>
    [DataField("radius", required: true)]
    public float Radius;

    [DataField("tileWeightList", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string TileWeightList = default!;

    /// <summary>
    /// Whether or not to smooth the results slightly.
    /// </summary>
    [DataField("smoothResult")]
    public bool SmoothResult = false;
}

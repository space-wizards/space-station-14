using Content.Server.Worldgen.Systems.Floorplanners;
using Content.Shared.Random;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Worldgen.Floorplanners;

public sealed record ScrapFloorplanConfig : FloorplanConfig
{
    public override Type FloorplannerSystem => typeof(ScrapFloorplannerSystem);

    [DataField("weatheringChange", required: true)]
    public float WeatheringChance;

    [DataField("wallEntityWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string WallEntityWeights = default!;

    [DataField("wallTileWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string WallTileWeights = default!;

    [DataField("WallEntityChance", required: true)]
    public float WallEntityChance;

    [DataField("floorEntityWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string FloorEntityWeights = default!;

    [DataField("floorTileWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string FloorTileWeights = default!;

    [DataField("floorEntityChance", required: true)]
    public float FloorEntityChance;

    [DataField("floorEntityPositionMaxOffset", required: true)]
    public float FloorEntityPositionMaxOffset = 0.5f; // half a tile in any direction.
}

using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Rooms;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage;

[Prototype("salvageExpedition")]
public sealed class SalvageExpeditionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("biome", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<BiomePrototype>))]
    public string Biome = string.Empty;

    [DataField("desc")]
    public string Description = string.Empty;

    [DataField("light")]
    public Color Light = Color.Black;

    [DataField("temperature")]
    public float Temperature = 293.15f;

    [DataField("expedition", required: true)] public ISalvageMission Expedition = default!;

    [DataField("minDuration")]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(9 * 60);

    [DataField("maxDuration")]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(12 * 60);

    /// <summary>
    /// Available factions for selection for this mission prototype.
    /// </summary>
    [DataField("factions")]
    public List<string> Factions = new();

    [DataField("dungeonConfig", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DungeonConfigPrototype>))]
    public string DungeonConfigPrototype = string.Empty;

    /// <summary>
    /// Possible loot prototypes available for this expedition.
    /// </summary>
    [DataField("loot", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string Loot = string.Empty;

    [DataField("dungeonRadius")]
    public float DungeonRadius = 50f;

    [DataField("dungeonPosition")]
    public Vector2i DungeonPosition = new(80, -25);
}

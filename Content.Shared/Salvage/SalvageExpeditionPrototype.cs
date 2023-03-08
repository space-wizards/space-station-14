using Content.Shared.Dataset;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Procedural.Rewards;
using Content.Shared.Random;
using Content.Shared.Salvage.Expeditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage;

[Prototype("salvageExpedition")]
public sealed class SalvageExpeditionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Naming scheme for the FTL marker.
    /// </summary>
    [DataField("nameProto", customTypeSerializer:typeof(PrototypeIdSerializer<DatasetPrototype>))]
    public string NameProto = "names_borer";

    /// <summary>
    /// Biome to generate the dungeon.
    /// </summary>
    [DataField("biome", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<BiomePrototype>))]
    public string Biome = string.Empty;

    /// <summary>
    /// Player-friendly description for the console.
    /// </summary>
    [DataField("desc")]
    public string Description = string.Empty;

    [DataField("difficultyRating")]
    public DifficultyRating DifficultyRating = DifficultyRating.Minor;

    // TODO: Make these modifiers but also add difficulty modifiers.
    [DataField("light")]
    public Color Light = Color.Black;

    [DataField("temperature")]
    public float Temperature = 293.15f;

    [DataField("expedition", required: true)]
    public ISalvageMission Mission = default!;

    [DataField("minDuration")]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(9 * 60);

    [DataField("maxDuration")]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(12 * 60);

    /// <summary>
    /// Available factions for selection for this mission prototype.
    /// </summary>
    [DataField("factions", customTypeSerializer:typeof(PrototypeIdListSerializer<SalvageFactionPrototype>))]
    public List<string> Factions = new();

    [DataField("dungeonConfig", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DungeonConfigPrototype>))]
    public string DungeonConfigPrototype = string.Empty;

    [DataField("reward", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string Reward = string.Empty;

    /// <summary>
    /// Possible loot prototypes available for this expedition.
    /// This spawns during the mission and is not tied to completion.
    /// </summary>
    [DataField("loot", customTypeSerializer: typeof(PrototypeIdListSerializer<WeightedRandomPrototype>))]
    public List<string> Loots = new();

    [DataField("dungeonPosition")]
    public Vector2i DungeonPosition = new(80, -25);
}

[Serializable, NetSerializable]
public enum DifficultyRating : byte
{
    None,
    Minor,
    Moderate,
    Hazardous,
    Extreme,
}


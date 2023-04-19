using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageDungeonMod")]
public sealed class SalvageDungeonMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    [DataField("proto", customTypeSerializer:typeof(PrototypeIdSerializer<DungeonConfigPrototype>))]
    public string Proto = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    /// <summary>
    /// Biomes this dungeon can occur in.
    /// </summary>
    [DataField("biomeMods", customTypeSerializer:typeof(PrototypeIdListSerializer<SalvageBiomeMod>))]
    public List<string>? BiomeMods;
}

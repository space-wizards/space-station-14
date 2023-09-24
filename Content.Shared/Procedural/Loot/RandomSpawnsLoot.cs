using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Randomly places loot in free areas inside the dungeon.
/// </summary>
public sealed partial class RandomSpawnsLoot : IDungeonLoot
{
    [ViewVariables(VVAccess.ReadWrite), DataField("entries", required: true)]
    public List<RandomSpawnLootEntry> Entries = new();
}

[DataDefinition]
public partial record struct RandomSpawnLootEntry : IBudgetEntry
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto { get; set; } = string.Empty;

    /// <summary>
    /// Cost for this loot to spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cost")]
    public float Cost { get; set; } = 1f;

    /// <summary>
    /// Unit probability for this entry. Weighted against the entire table.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prob")]
    public float Prob { get; set; } = 1f;
}

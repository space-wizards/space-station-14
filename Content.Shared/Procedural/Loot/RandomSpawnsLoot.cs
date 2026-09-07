using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Randomly places loot in free areas inside the dungeon.
/// </summary>
public sealed partial class RandomSpawnsLoot : IDungeonLoot
{
    [DataField(required: true)]
    public List<RandomSpawnLootEntry> Entries = new();
}

[DataDefinition]
public partial record struct RandomSpawnLootEntry() : IBudgetEntry
{
    [DataField(required: true)]
    public EntProtoId Proto { get; set; }

    /// <summary>
    /// Cost for this loot to spawn.
    /// </summary>
    [DataField]
    public float Cost { get; set; } = 1f;

    /// <summary>
    /// Unit probability for this entry. Weighted against the entire table.
    /// </summary>
    [DataField]
    public float Prob { get; set; } = 1f;
}

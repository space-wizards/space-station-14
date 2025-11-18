using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared.Procedural.DungeonLayers;


/// <summary>
/// Spawns entities inside of the dungeon randomly.
/// </summary>
public sealed partial class EntityTableDunGen : IDunGenLayer
{
    // Counts separate to config to avoid some duplication.

    [DataField]
    public int MinCount = 1;

    [DataField]
    public int MaxCount = 1;

    [DataField(required: true)]
    public EntityTableSelector Table;

    /// <summary>
    /// Should the count be per dungeon or across all dungeons.
    /// </summary>
    [DataField]
    public bool PerDungeon;
}

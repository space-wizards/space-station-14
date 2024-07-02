using Content.Shared.Storage;

namespace Content.Shared.Procedural.DungeonLayers;


/// <summary>
/// Spawns mobs inside of the dungeon randomly.
/// </summary>
public sealed partial class MobsDunGen : IDunGenLayer
{
    // Counts separate to config to avoid some duplication.

    [DataField]
    public int MinCount = 1;

    [DataField]
    public int MaxCount = 1;

    [DataField(required: true)]
    public List<EntitySpawnEntry> Groups = new();
}

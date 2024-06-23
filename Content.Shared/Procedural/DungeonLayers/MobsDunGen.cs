using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonLayers;


/// <summary>
/// Spawns mobs inside of the dungeon randomly.
/// </summary>
public sealed partial class MobsDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public List<MobGroup> Groups = new();
}

/// <summary>
/// Mob groups for spawning.
/// </summary>
public record struct MobGroup()
{
    [DataField]
    public int MinCount = 1;

    [DataField]
    public int MaxCount = 1;

    [DataField]
    public ProtoId<WeightedRandomPrototype> Proto;
}

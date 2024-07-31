using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// This component spawns entities from a WeightedRandomEntityPrototype(aka entity table).
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
[Virtual]
public partial class GenericSpawnerComponent : Component
{
    /// <summary>
    /// WeightedRandomEntityPrototype IDs from which the entity will be picked, along with their respective chances.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<WeightedRandomEntityPrototype>, float> EntityTables = new();

    /// <summary>
    /// Which gamerules have to be active for that spawner to work.
    /// </summary>
    [DataField]
    public List<EntProtoId> GameRules = new();

    /// <summary>
    /// Spawned entities get spread randomly in a square with this size. Set to 0 to disable.
    /// </summary>
    [DataField]
    public float Offset { get; set; } = 0.2f;

    /// <summary>
    /// Spawner will pick an entity this amount of times. Must be between 1 and 100.
    /// </summary>
    [DataField]
    public int Rolls { get; set; } = 1;

    /// <summary>
    /// Should this spawner be deleted after spawning an entity?
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;

}

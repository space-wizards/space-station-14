using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class WeightedSpawnEntity : EntityEffectBase<WeightedSpawnEntity>
{
    /// <summary>
    /// A table of entities with assigned weights to randomly pick from.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomEntityPrototype> WeightedEntityTable;

    /// <summary>
    /// How far away to spawn the entity from the parent position.
    /// </summary>
    [DataField]
    public float SpawnOffset = 1;

    /// <summary>
    /// The minimum number of entities to spawn randomly.
    /// </summary>
    [DataField]
    public int MinSpawn = 1;

    /// <summary>
    /// The maximum number of entities to spawn randomly.
    /// </summary>
    [DataField]
    public int MaxSpawn = 1;

    /// <summary>
    /// Time in seconds to wait before spawning entities.
    /// </summary>
    [DataField]
    public float SpawnAfter;
}

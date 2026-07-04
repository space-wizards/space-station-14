using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Destructible;

/// <summary>
/// This entity will spawn things at its location when getting *destroyed* through <see cref="SharedDestructibleSystem"/>.
/// </summary>
/// <remarks>
/// This component recreates the spawning functionality from <see cref="DestructibleComponent"/> thresholds,
/// specifically <c>SpawnEntitiesBehavior</c> and <c>WeightedSpawnEntityBehavior</c>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDestructibleSystem))]
public sealed partial class SpawnOnDestroyedComponent : Component
{
    // Filthy dummy entity for spawning delayed.
    public static readonly EntProtoId TempEntity = "TemporaryEntityForTimedDespawnSpawners";

    /// <summary>
    /// Entities that will spawn from this entity.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Spawn;

    /// <summary>
    /// Time in seconds to wait before spawning entities. Useful when your entity also explodes.
    /// </summary>
    /// <remarks>
    /// Overrides forensics transfering and won't spawn in containers.
    /// </remarks>
    [DataField]
    public TimeSpan? SpawnAfter;

    /// <summary>
    /// How far from the destroyed entity to spawn.
    /// </summary>
    [DataField]
    public float Offset = 0.5f;

    /// <summary>
    /// Chance for forensics to be transferred.
    /// Transferring is skipped if null.
    /// </summary>
    [DataField]
    public float? ForensicsChance = .4f;

}

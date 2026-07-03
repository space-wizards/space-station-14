using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Destructible;

/// <summary>
/// This entity will spawn things at its location while getting *destroyed*.
/// </summary>
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
    /// Overrides <see cref="TransferForensics"/> and won't spawn in containers.
    /// </remarks>
    [DataField]
    public TimeSpan? SpawnAfter;

    /// <summary>
    /// How far from the destroyed entity to spawn.
    /// </summary>
    [DataField]
    public float? Offset = 0.5f;

    /// <summary>
    /// Spawned items will try to copy the forensics of the destroyed entity.
    /// </summary>
    [DataField]
    public bool TransferForensics = true;

    /// <summary>
    /// Chance for forensics to be transferred if <see cref="TransferForensics"/> is true.
    /// </summary>
    [DataField]
    public float ForensicsChance = .4f;

}

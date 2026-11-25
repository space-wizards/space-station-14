using Content.Server.Spawners.EntitySystems;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// A spawner that randomly spawns entities according to an <see cref="EntityTableSelector"/>.
/// </summary>
[RegisterComponent, EntityCategory("Spawner"), Access(typeof(ConditionalSpawnerSystem))]
public sealed partial class EntityTableSpawnerComponent : Component
{
    /// <summary>
    /// Table that determines what gets spawned.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Maximum distance in meters to scatter spawned entities from the spawner.
    /// </summary>
    /// <remarks>
    /// Spawned entities are created in a disk this size around the spawner.
    /// </remarks>
    [DataField]
    public float Offset = 0.2f;

    /// <summary>
    /// A variable meaning whether the spawn will
    /// be able to be used again or whether
    /// it will be destroyed after the first use
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;

    /// <summary>
    /// Marker, if produced entities should be spawned stacked if they could be.
    /// </summary>
    [DataField]
    public bool AutoStack = false;
}


using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class EntityTableSpawnerComponent : Component
{
    /// <summary>
    /// Table that determines what gets spawned.
    /// </summary>
    [DataField]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Scatter of entity spawn coordinates
    /// </summary>
    [DataField]
    public float Offset = 0.2f;

    /// <summary>
    /// A variable meaning whether the spawn will
    /// be able to be used again or whether
    /// it will be destroyed after the first use
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;
}


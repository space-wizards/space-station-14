using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// Spawns entities if the tile they're about to spawn on borders a pure space tile
/// (not a scaffold or lattice tile).
/// </summary>
[RegisterComponent]
public sealed partial class GridEdgeSpawnComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;

    /// <summary>
    /// Whether to delete the spawner upon spawning the entity.
    /// </summary>
    [DataField]
    public bool DeleteSpawner = true;
}

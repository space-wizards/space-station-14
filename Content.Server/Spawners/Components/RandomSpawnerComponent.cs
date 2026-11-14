using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

/// <summary>
/// An extended <see cref="ConditionalSpawnerComponent"/> with optional rare prototypes and more configurable spawn behavior.
/// </summary>
/// <remarks>
/// For non-trivial lists of prototypes, consider using <see cref="EntityTableSpawnerComponent"/> and an <see cref="EntityTableSelector"/> instead.
/// </remarks>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class RandomSpawnerComponent : ConditionalSpawnerComponent
{
    /// <summary>
    /// A list of rare entities to spawn.
    /// </summary>
    /// <remarks>
    /// On a successful spawn roll, with <see cref="RareChance"/> probability,
    /// an entity from this list is spawned (vs. the normal Prototypes list).
    /// </summary>
    [DataField]
    public List<EntProtoId> RarePrototypes { get; set; } = new();

    /// <summary>
    /// The chance that a rare prototype may spawn instead of a common prototype.
    /// </summary>
    [DataField]
    public float RareChance { get; set; } = 0.05f;

    /// <summary>
    /// Maximum distance in meters to scatter spawned entities from the spawner.
    /// </summary>
    /// <remarks>
    /// Spawned entities are created in a disk this size around the spawner.
    /// </remarks>
    [DataField]
    public float Offset { get; set; } = 0.2f;

    /// <summary>
    /// If true, the spawner is deleted on <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;
}

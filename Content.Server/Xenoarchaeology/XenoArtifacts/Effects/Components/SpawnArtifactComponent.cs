using Content.Shared.Storage;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     When activated artifact will spawn an entity from prototype.
///     It could be an angry mob or some random item.
/// </summary>
[RegisterComponent]
public sealed class SpawnArtifactComponent : Component
{
    [DataField("spawns")]
    public List<EntitySpawnEntry>? Spawns;

    /// <summary>
    /// The range around the artifact that it will spawn the entity
    /// </summary>
    [DataField("range")]
    public float Range = 0.5f;

    /// <summary>
    /// The maximum number of times the spawn will occur
    /// </summary>
    [DataField("maxSpawns")]
    public int MaxSpawns = 10;
}

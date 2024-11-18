using Content.Shared.Storage;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     When activated artifact will spawn an entity from prototype.
///     It could be an angry mob or some random item.
/// </summary>
[RegisterComponent, Access(typeof(XAESpawnSystem))]
public sealed partial class XAESpawnComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry>? Spawns;

    /// <summary>
    /// The range around the artifact that it will spawn the entity
    /// </summary>
    [DataField]
    public float Range = 0.5f;

    /// <summary>
    /// The maximum number of times the spawn will occur
    /// </summary>
    [DataField]
    public int MaxSpawns = 10;
}

using Robust.Shared.Prototypes;

namespace Content.Server.Destructible.Thresholds.Behaviors;

/// <summary>
///     Spawn the given entities.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SimpleSpawnEntitiesBehavior : BaseSpawnEntitiesBehavior
{
    /// <summary>
    ///     Entities spawned by this behavior, and how many.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Spawn = new();

    protected override Dictionary<EntProtoId, int> GetSpawns(DestructibleSystem system, EntityUid owner)
    {
        return Spawn; // Doesn't get simpler
    }
}

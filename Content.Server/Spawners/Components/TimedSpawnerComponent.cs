using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Spawners.Components;

/// <summary>
/// Spawns entities at a set interval.
/// Can configure the set of entities, spawn timing, spawn chance,
/// and min/max number of entities to spawn.
/// </summary>
[RegisterComponent]
public sealed partial class TimedSpawnerComponent : Component, ISerializationHooks
{
    /// <summary>
    /// List of entities that can be spawned by this component. One will be randomly
    /// chosen for each entity spawned. When multiple entities are spawned at once,
    /// each will be randomly chosen separately.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = [];

    /// <summary>
    /// Chance of an entity being spawned at the end of each interval.
    /// </summary>
    [DataField]
    public float Chance = 1.0f;

    /// <summary>
    /// Length of the interval between spawn attempts.
    /// </summary>
    [DataField]
    public int IntervalSeconds = 60;

    /// <summary>
    /// The minimum number of entities that can be spawned when an interval elapses.
    /// </summary>
    [DataField]
    public int MinimumEntitiesSpawned = 1;

    /// <summary>
    /// The maximum number of entities that can be spawned when an interval elapses.
    /// </summary>
    [DataField]
    public int MaximumEntitiesSpawned = 1;

    public CancellationTokenSource? TokenSource;

    void ISerializationHooks.AfterDeserialization()
    {
        if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
            throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
    }
}

using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Used an event that spawns an anomaly somewhere random on the map.
/// </summary>
[RegisterComponent, Access(typeof(AnomalySpawnRule))]
public sealed partial class AnomalySpawnRuleComponent : Component
{
    [DataField]
    public EntProtoId AnomalySpawnerPrototype = "RandomAnomalySpawner";

    /// <summary>
    /// How many anomalies should be spawned?
    /// </summary>
    [DataField]
    public MinMax AmountToSpawn = new MinMax(1, 1);
}

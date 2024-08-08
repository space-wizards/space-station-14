using Content.Server.StationEvents.Events;
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
}

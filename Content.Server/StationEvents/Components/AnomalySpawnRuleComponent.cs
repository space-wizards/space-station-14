using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Used an event that spawns an anomaly somewhere random on the map.
/// </summary>
[RegisterComponent]
public sealed class AnomalySpawnRuleComponent : Component
{
    [DataField("anomalySpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AnomalySpawnerPrototype = "RandomAnomalySpawner";
}

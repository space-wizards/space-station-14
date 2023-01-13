using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for a machine that is able to generate
/// anomalies randomly on the station.
/// </summary>
[RegisterComponent]
public sealed class AnomalyGeneratorComponent : Component
{
    /// <summary>
    /// The random anomaly spawner
    /// </summary>
    [DataField("spawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnerPrototype = "RandomAnomalySpawner";
}

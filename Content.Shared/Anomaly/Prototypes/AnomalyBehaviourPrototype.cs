using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Prototypes;

[Prototype("anomalyBehaviour")]
public sealed partial class AnomalyBehaviourPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public string Description { get; private set; } = string.Empty;

    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();
}

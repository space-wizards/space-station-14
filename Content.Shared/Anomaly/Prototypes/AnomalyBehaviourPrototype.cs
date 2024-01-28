using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Prototypes;

[Prototype("anomalyBehaviour")]
public sealed partial class AnomalyBehaviourPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public float PulseFrequencyModifier = 1f;

    [DataField]
    public float EarnPointModifier = 1f;

    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();
}

using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Prototypes;

[Prototype("anomalyBehaviour")]
public sealed partial class AnomalyBehaviourPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Description for anomaly scanner
    /// </summary>
    [DataField]
    public string Description = string.Empty;

    /// <summary>
    /// The weight of the chance that an anomaly will exhibit this particular behavior
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// modification of the number of points earned from an anomaly
    /// </summary>
    [DataField]
    public float EarnPointModifier = 1f;

    /// <summary>
    /// deceleration or acceleration of the pulsation frequency of the anomaly
    /// </summary>
    [DataField]
    public float PulseFrequencyModifier = 1f;

    /// <summary>
    /// Pulsation amplification multiplier
    /// </summary>
    [DataField]
    public float PulsePowerModifier = 1f;

    /// <summary>
    /// Components that are added to the anomaly when this behavior is selected, and removed when another behavior is selected.
    /// </summary>
    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();
}

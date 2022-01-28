using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Prototypes;

[Prototype("dynamicScheduler")]
public class DynamicSchedulerPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    [DataField("minRoundTime")]
    public float MinRoundTime = 0.0f;

    [DataField("maxRoundTime")]
    public float MaxRoundTime = 60.0f * 60.0f;

    /// <summary>
    ///     How often will this scheduler run in seconds?
    /// </summary>
    [DataField("frequency")]
    public float Frequency = 5.0f * 60.0f;


}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components.OnTrigger;

/// <summary>
/// After being triggered applies the specified components and runs triggers again.
/// </summary>
[RegisterComponent]
public sealed partial class TwoStageTriggerComponent : Component
{
    /// <summary>
    /// How long it takes for the second stage to be triggered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("triggerDelay")]
    public TimeSpan TriggerDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// This list of components that will be added for the second trigger.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("components", required: true)]
    public ComponentRegistry SecondStageComponents = new();

    [DataField("nextTriggerTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextTriggerTime;

    [ViewVariables(VVAccess.ReadWrite), DataField("triggered")] public bool Triggered = false;
}

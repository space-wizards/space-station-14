using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Constantly triggers after being added to an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RepeatingTriggerComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// How long to wait between triggers.
    /// The first trigger starts this long after the component is added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When the next trigger will be.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTrigger = TimeSpan.Zero;
}

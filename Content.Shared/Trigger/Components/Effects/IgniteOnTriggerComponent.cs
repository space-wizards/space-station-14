using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will ignite for a certain length of time when triggered.
/// Requires <see cref="IgnitionSourceComponent"/> along with triggering components.
/// The if TargetUser is true they will be ignited instead (they need IgnitionSourceComponent as well).
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class IgniteOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Once ignited, the time it will unignite at.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan IgnitedUntil = TimeSpan.Zero;

    /// <summary>
    /// How long the ignition source is active for after triggering.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan IgnitedTime = TimeSpan.FromSeconds(0.5);
}

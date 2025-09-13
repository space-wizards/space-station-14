using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// Exposes timer trigger delay information via item status control.
/// </summary>
/// <seealso cref="TimerTriggerItemStatusSyncSystem"/>
/// <seealso cref="TimerTriggerStatusControl"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimerTriggerItemStatusComponent : Component
{
    /// <summary>
    /// Current set delay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay;
}

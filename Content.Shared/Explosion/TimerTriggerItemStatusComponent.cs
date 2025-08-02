using Robust.Shared.GameStates;

namespace Content.Shared.Explosion;

/// <summary>
/// Exposes timer trigger delay information via item status control.
/// Synced to clients to display set delay for explosives and similar items.
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

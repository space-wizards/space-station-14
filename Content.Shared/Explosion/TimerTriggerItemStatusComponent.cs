using Robust.Shared.GameStates;

namespace Content.Shared.Explosion;

/// <summary>
/// Exposes timer trigger delay information via item status control.
/// Synced to clients to display set delay for explosives and similar items.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimerTriggerItemStatusComponent : Component
{
    /// <summary>
    /// Current set delay in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Trigger effect for stunning an entity.
/// Stuns the user if <see cref="BaseXOnTriggerComponent.TargetUser"/> is true.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StunOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// How long to stun the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunAmount = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If true, refresh the stun duration.
    /// If false, stun is added on-top of any existing stun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Refresh = true;
}

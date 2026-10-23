using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Trigger effect for sending the target sidewise (crawling).
/// Knockdowns the user if <see cref="BaseXOnTriggerComponent.TargetUser"/> is true.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnockdownOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// How long the target is forced to be on the ground.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownAmount =  TimeSpan.FromSeconds(1);

    /// <summary>
    /// If true, refresh the duration.
    /// If false, time is added on-top of any existing forced knockdown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Refresh = true;

    /// <summary>
    /// Should the entity try and stand automatically?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoStand = true;

    /// <summary>
    /// Should the entity drop their items upon first being knocked down?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Drop = true;
}

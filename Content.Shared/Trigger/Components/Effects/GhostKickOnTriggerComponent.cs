using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will kick a player from the server as if their connection dropped if triggered.
/// Yes, really. Don't use this component.
/// If TargetUser is true then the user of the trigger will be kicked, otherwise the entity itself.
/// <see cref="Server.GhostKick.GhostKickManager"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostKickOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The reason that will be displayed in the server log when a player is kicked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Reason = "ghost-kick-on-trigger-default";
}

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Checks if a triggered entity or the user of a trigger has a certain mindrole.
/// Cancels the trigger otherwise.
/// </summary>
/// <remarks>
/// Mind roles are only networked to their owner! So if you use this on any other entity than yourself it won't be predicted.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindRoleTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Whitelist for what mind role components on the owning entity allow this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    /// Whitelist for what mind role components on the User allow this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? UserWhitelist;
}

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Checks if the user of a trigger satisfies a whitelist and blacklist condition for the triggered entity or the one triggering it.
/// Cancels the trigger otherwise.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WhitelistTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Whitelist for what entites can cause this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    /// Blacklist for what entites can cause this trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? UserBlacklist;
}

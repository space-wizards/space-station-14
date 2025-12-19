using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

/// <summary>
/// Trigger when gamerule actually begins
/// </summary>
public sealed partial class TriggerOnGameRuleComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Valid gamerules
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Invalid gamerules
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}


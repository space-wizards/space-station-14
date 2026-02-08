using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// Checks that the tripper is pass Whitelist and Blacklist to trigger.
/// </summary>
/// <remarks>
/// Empty Whitelist and Blacklist makes always continue step trigger attempt.
/// Can have both Whitelist and Blacklist. Checks Whitelist at first and Blacklist after that.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepWhitelistAttemptComponent : BaseStepTriggerOnXComponent
{
    /// <summary>
    /// The whitelist that the tripper must match.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The blacklist that the tripper must match.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}

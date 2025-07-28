using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// Unique Whitelist and Blacklist check that requires <see cref="TriggerStepLogicComponent"/>.
/// If Whitelist is not empty, and only if any entities occupy the Whitelist on the same tile then step trigger will work.
/// If Blacklist is not empty, and if any entities occupy the Blacklist on the same tile then step trigger won't work.
/// </summary>
/// <remarks>
/// Can have both Whitelist and Blacklist. Checks Whitelist at first and Blacklist after that.
/// Basically, used for lava and water with catwalks.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerStepLogicWithWhitelistComponent : BaseStepTriggerOnXComponent
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

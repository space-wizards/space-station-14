using Content.Shared.EntityConditions;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Checks for an EntityCondition before doing the action
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionRequiresConditionSystem))]
public sealed partial class ActionRequiresConditionComponent : Component
{
    /// <summary>
    /// Conditions that will be checked
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// Popup displayed if the conditions fail
    /// </summary>
    [DataField]
    public LocId? FailureMessage;
}

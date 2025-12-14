using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Cancels the trigger if it's user and target are the same.
/// Useful for preventing triggering things on yourself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UserTargetTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Will cause the trigger to cancel if the user ISN'T the same as the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Invert = false;
}

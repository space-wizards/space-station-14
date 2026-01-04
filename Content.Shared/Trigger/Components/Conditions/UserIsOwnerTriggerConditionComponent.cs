using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Cancels the trigger if the owner of this component and the trigger user are NOT the same.
/// Useful for preventing triggering things on yourself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UserIsOwnerTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Will cause the trigger to cancel if the user IS the same as the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Invert = false;
}

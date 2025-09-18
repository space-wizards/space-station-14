using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// For internal usage only, this component added when entities are stepped on.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepTriggerActiveComponent : BaseStepTriggerOnXComponent
{
    /// <summary>
    /// Enables or disables TriggerActive.
    /// </summary>
    /// <remarks>
    /// VERY important for prediction purposes.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool IsActive = true;
}

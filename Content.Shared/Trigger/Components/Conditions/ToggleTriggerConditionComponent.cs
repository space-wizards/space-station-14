using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Adds an alt verb that can be used to toggle a trigger.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// Is the component currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The text of the toggle verb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ToggleVerb = "toggle-trigger-condition-default-verb";

    /// <summary>
    /// The popup to show when toggled on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ToggleOn = "toggle-trigger-condition-default-on";

    /// <summary>
    /// The popup to show when toggled off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ToggleOff = "toggle-trigger-condition-default-off";
}

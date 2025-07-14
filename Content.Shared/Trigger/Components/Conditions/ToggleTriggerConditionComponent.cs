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
    [DataField]
    public LocId ToggleVerb = "verb-toggle-start-on-stick";

    /// <summary>
    /// The text of the toggle verb.
    /// </summary>
    [DataField]
    public LocId ToggleOn = "verb-toggle-start-on-stick";

    /// <summary>
    /// The text of the toggle verb.
    /// </summary>
    [DataField]
    public LocId ToggleOff = "verb-toggle-start-on-stick";
}

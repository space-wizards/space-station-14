using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Starts a trigger when a verb is selected.
/// The user is the player selecting the verb.
/// </summary>
/// <remarks>
/// TODO: Support multiple verbs and trigger keys.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnVerbComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The text to display in the verb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Text = "trigger-on-verb-default";

    /// <summary>
    /// The priority of the verb. The alternative verb with the highest priority is used on alt-click.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority = 2; // should be above any timer settings
}

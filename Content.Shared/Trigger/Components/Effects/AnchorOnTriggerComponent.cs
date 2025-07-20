using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will (un)anchor the entity when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnchorOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Anchor the entity on trigger if it is currently unanchored?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanAnchor = true;

    /// <summary>
    /// Unanchor the entity on trigger if it is currently anchored?
    /// If both this and CanAnchor are true then the trigger will toggle between states.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanUnanchor = false;

    /// <summary>
    /// Removes this component when triggered so it can only be activated once.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveOnTrigger = true;
}

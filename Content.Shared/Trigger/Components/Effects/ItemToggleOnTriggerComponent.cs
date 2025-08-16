using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will toggle an item when triggered. Requires <see cref="ItemToggleComponent"/>.
/// If TargetUser is true and they have that component they will be toggled instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Can the item be toggled on using the trigger?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanActivate = true;

    /// <summary>
    /// Can the item be toggled on using the trigger?
    /// If both this and CanActivate are true then the trigger will toggle between states.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanDeactivate = true;

    /// <summary>
    /// Can the audio and popups be predicted?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Predicted = true;

    /// <summary>
    /// Show a popup to the user when toggling the item?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowPopup = true;
}

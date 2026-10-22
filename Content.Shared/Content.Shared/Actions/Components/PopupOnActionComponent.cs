using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Displays a popup message when the action is successfully performed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PopupOnActionSystem))]
public sealed partial class PopupOnActionComponent : Component
{
    /// <summary>
    /// The locale ID of the message to the performer of the action.
    /// </summary>
    [DataField]
    public LocId? SelfMessage;

    /// <summary>
    /// The message to show to those around the performer of the action.
    /// </summary>
    [DataField]
    public LocId? OthersMessage;

    /// <summary>
    /// The message to show to the action target.
    /// </summary>
    [DataField]
    public LocId? TargetMessage;

    /// <summary>
    /// The visual style of the popup.
    /// </summary>
    [DataField]
    public PopupType PopupType = PopupType.Small;
}

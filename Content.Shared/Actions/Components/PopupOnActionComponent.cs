using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Displays a popup message when the action is successfully performed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PopupOnActionSystem))]
public sealed partial class PopupOnActionComponent : Component
{
    /// <summary>
    /// The popup message shown to the action performer.
    /// </summary>
    [DataField]
    public PopupMessage? UserMessage;

    /// <summary>
    /// The popup message shown to the target of the action.
    /// </summary>
    [DataField]
    public PopupMessage? TargetMessage;

    /// <summary>
    /// The visual style of the popup.
    /// </summary>
    [DataField]
    public PopupType PopupType = PopupType.Small;
}

/// <summary>
/// Defines a popup message and who can see it.
/// </summary>
[DataDefinition]
public sealed partial class PopupMessage
{
    /// <summary>
    /// The locale ID of the message to display.
    /// </summary>
    [DataField(required: true)]
    public LocId Message = string.Empty;

    /// <summary>
    /// Determines who can see this popup.
    /// </summary>
    [DataField]
    public PopupRecipients Recipients = PopupRecipients.Local;
}

/// <summary>
/// Who can see the popup message.
/// </summary>
[Serializable, NetSerializable]
public enum PopupRecipients : byte
{
    Pvs,
    Local,
}

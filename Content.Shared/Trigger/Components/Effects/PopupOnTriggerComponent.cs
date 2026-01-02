using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Displays a popup on the target when triggered.
/// Supports following fluent variables:
///     $entity - displays the target entity's name
///     $user - displays the user's name
/// Will display the popup on the user when <see cref="BaseXOnTriggerComponent.TargetUser"/> is true.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PopupOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The text this popup will display to the recipient.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Text;

    /// <summary>
    /// The text this popup will display to everything but the recipient.
    /// If left null this will reuse <see cref="Text"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? OtherText;

    /// <summary>
    /// The size and color of the popup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PopupType PopupType = PopupType.Small;

    /// <summary>
    /// If true, the user is the recipient of the popup.
    /// If false, this entity is the recipient.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserIsRecipient = true;

    /// <summary>
    /// If true, this popup will only play for the recipient and ignore prediction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Quiet;

    /// <summary>
    /// Whether to use predicted popups.
    /// </summary>
    /// <remarks>If false, this will spam any client that causes this trigger.</remarks>
    [DataField, AutoNetworkedField]
    public bool Predicted = true;
}

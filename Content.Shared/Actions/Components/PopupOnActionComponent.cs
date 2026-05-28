using Content.Shared.EntityEffects.Effects.Transform;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Displays a popup message when the action is successfully performed.
/// Uses <see cref="PopupMessage"/> entity effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PopupOnActionSystem))]
public sealed partial class PopupOnActionComponent : Component
{
    /// <summary>
    /// Who receives the popup message.
    /// </summary>
    [DataField]
    public PopupRecipient Recipient = PopupRecipient.User;

    /// <summary>
    /// Popup message effect to apply.
    /// </summary>
    [DataField(required: true)]
    public PopupMessage Popup;
}

/// <summary>
/// Who receives the popup message.
/// </summary>
public enum PopupRecipient : byte
{
    User,
    Target,
}

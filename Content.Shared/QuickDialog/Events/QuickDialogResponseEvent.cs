using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.Events;

/// <summary>
/// A networked event raised when the client replies to a quick dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogResponseEvent(string dialogId, object?[] responses, QuickDialogButtonFlag buttonPressed) : EntityEventArgs
{
    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public readonly string DialogId = dialogId;

    /// <summary>
    /// The responses to the prompts.
    /// </summary>
    public readonly object?[]? Responses = responses;

    /// <summary>
    /// The button pressed when responding.
    /// </summary>
    public readonly QuickDialogButtonFlag ButtonPressed = buttonPressed;
}

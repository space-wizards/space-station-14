using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.BUI;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogResponseMessage(object?[] responses, QuickDialogButtonFlag buttonPressed) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The responses to the prompts.
    /// </summary>
    public readonly object?[]? Responses = responses;

    /// <summary>
    /// The button pressed when responding.
    /// </summary>
    public readonly QuickDialogButtonFlag ButtonPressed = buttonPressed;
}

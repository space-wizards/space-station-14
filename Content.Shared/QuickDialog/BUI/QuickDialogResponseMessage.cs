using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.BUI;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogResponseMessage(QuickDialogButtonFlags buttonPressed, string[]? responses = null) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The button pressed when responding.
    /// </summary>
    public readonly QuickDialogButtonFlags ButtonPressed = buttonPressed;

    /// <summary>
    /// The responses to the prompts.
    /// </summary>
    public readonly string[]? Responses = responses;
}

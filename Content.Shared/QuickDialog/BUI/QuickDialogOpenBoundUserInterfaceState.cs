using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.BUI;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogOpenBoundUserInterfaceState(string title, IQuickDialogEntry[] prompts, QuickDialogButtonFlags buttons) : BoundUserInterfaceState
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public readonly string Title = title;

    /// <summary>
    /// The prompts to show the user.
    /// </summary>
    public readonly IQuickDialogEntry[] Prompts = prompts;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public readonly QuickDialogButtonFlags Buttons = buttons;
}

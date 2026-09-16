using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.BUI;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogBoundUserInterfaceState(string title, IQuickDialogEntry[] entries, QuickDialogButtonFlags buttons) : BoundUserInterfaceState
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public readonly string Title = title;

    /// <summary>
    /// The entries to show the user.
    /// </summary>
    public readonly IQuickDialogEntry[] Entries = entries;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public readonly QuickDialogButtonFlags Buttons = buttons;
}

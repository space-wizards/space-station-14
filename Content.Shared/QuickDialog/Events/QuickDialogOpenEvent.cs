using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog.Events;

/// <summary>
/// A networked event raised when the server wants to open a quick dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogOpenEvent(string dialogId, string title, IQuickDialogEntry[] entries, QuickDialogButtonFlags buttons) : EntityEventArgs
{
    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public readonly string DialogId = dialogId;

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


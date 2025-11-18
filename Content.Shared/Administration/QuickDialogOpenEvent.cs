using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// A networked event raised when the server wants to open a quick dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogOpenEvent : EntityEventArgs
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Title;

    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public int DialogId;

    /// <summary>
    /// The prompts to show the user.
    /// </summary>
    public List<QuickDialogEntry> Prompts;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public QuickDialogButtonFlag Buttons = QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton;

    public QuickDialogOpenEvent(string title, List<QuickDialogEntry> prompts, int dialogId, QuickDialogButtonFlag buttons)
    {
        Title = title;
        Prompts = prompts;
        Buttons = buttons;
        DialogId = dialogId;
    }
}

/// <summary>
/// A networked event raised when the client replies to a quick dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogResponseEvent : EntityEventArgs
{
    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public int DialogId;

    /// <summary>
    /// The responses to the prompts.
    /// </summary>
    public Dictionary<string, string> Responses;

    /// <summary>
    /// The button pressed when responding.
    /// </summary>
    public QuickDialogButtonFlag ButtonPressed;

    public QuickDialogResponseEvent(int dialogId, Dictionary<string, string> responses, QuickDialogButtonFlag buttonPressed)
    {
        DialogId = dialogId;
        Responses = responses;
        ButtonPressed = buttonPressed;
    }
}

/// <summary>
/// An entry in a quick dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class QuickDialogEntry
{
    /// <summary>
    /// ID of the dialog field.
    /// </summary>
    public string FieldId;

    /// <summary>
    /// Type of the field, for checks.
    /// </summary>
    public QuickDialogEntryType Type;

    /// <summary>
    /// The prompt to show the user.
    /// </summary>
    public string Prompt;

    /// <summary>
    /// String to replace the type-specific placeholder with.
    /// </summary>
    public string? Placeholder;

    public QuickDialogEntry(string fieldId, QuickDialogEntryType type, string prompt, string? placeholder = null)
    {
        FieldId = fieldId;
        Type = type;
        Prompt = prompt;
        Placeholder = placeholder;
    }
}

/// <summary>
/// The buttons available in a quick dialog.
/// </summary>
[Flags]
public enum QuickDialogButtonFlag
{
    OkButton = 1,
    CancelButton = 2,
}

/// <summary>
/// The entry types for a quick dialog.
/// </summary>
public enum QuickDialogEntryType
{
    /// <summary>
    /// Any integer.
    /// </summary>
    Integer,
    /// <summary>
    /// Any floating point value.
    /// </summary>
    Float,
    /// <summary>
    /// Maximum of 100 characters string.
    /// </summary>
    ShortText,
    /// <summary>
    /// Maximum of 2,000 characters string.
    /// </summary>
    LongText,
}

using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class QuickDialogOpenEvent : EntityEventArgs
{
    public string Title;

    public int DialogId;

    public List<QuickDialogEntry> Prompts;

    public QuickDialogButtonFlag Buttons = QuickDialogButtonFlag.OkButton;

    public QuickDialogOpenEvent(string title, List<QuickDialogEntry> prompts, int dialogId, QuickDialogButtonFlag buttons)
    {
        Title = title;
        Prompts = prompts;
        Buttons = buttons;
        DialogId = dialogId;
    }
}

[Serializable, NetSerializable]
public sealed class QuickDialogResponseEvent : EntityEventArgs
{
    public int DialogId;

    public Dictionary<string, string> Responses;

    public QuickDialogButtonFlag ButtonPressed;

    public QuickDialogResponseEvent(int dialogId, Dictionary<string, string> responses, QuickDialogButtonFlag buttonPressed)
    {
        DialogId = dialogId;
        Responses = responses;
        ButtonPressed = buttonPressed;
    }
}

[Serializable, NetSerializable]
public sealed class QuickDialogEntry
{
    public string FieldId;

    public QuickDialogEntryType Type;

    public string Prompt;

    public QuickDialogEntry(string fieldId, QuickDialogEntryType type, string prompt)
    {
        FieldId = fieldId;
        Type = type;
        Prompt = prompt;
    }
}

[Flags]
public enum QuickDialogButtonFlag
{
    OkButton = 1,
    CancelButton = 2,
}

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

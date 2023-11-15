namespace Content.Shared.TextScreen.Events;

/// <summary>
/// Sets the Text on a TextScreen, and sets its display mode to Text.
/// </summary>
[ByRefEvent]
public readonly record struct TextScreenTextEvent(string?[] Text)
{
    public readonly string?[] Text = Text;
}

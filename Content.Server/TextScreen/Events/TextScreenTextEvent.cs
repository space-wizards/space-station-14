namespace Content.Server.TextScreen.Events;

/// <summary>
/// Sets the Label on a TextScreen, and sets its display mode to Text.
/// </summary>
[ByRefEvent]
public readonly record struct TextScreenTextEvent(string Label)
{
    public readonly string Label = Label;
}

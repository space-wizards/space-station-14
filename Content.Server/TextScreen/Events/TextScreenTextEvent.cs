namespace Content.Server.TextScreen.Events;

[ByRefEvent]
public readonly record struct TextScreenTextEvent(string Label)
{
    public readonly string Label = Label;
}

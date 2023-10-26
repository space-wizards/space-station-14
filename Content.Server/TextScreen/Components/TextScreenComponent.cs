namespace Content.Server.TextScreen.Components;

[RegisterComponent]
public sealed partial class TextScreenComponent : Component
{
    [DataField("label"), ViewVariables]
    public string Label { get; set; } = string.Empty;

    [DataField("doneSound"), ViewVariables]
    public string? DoneSound;
}

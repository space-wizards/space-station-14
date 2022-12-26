namespace Content.Client.Paper;

[RegisterComponent]
public sealed class PaperVisualsComponent : Component
{
    //<todo.eoin Probably want to use a Sprite here?
    [DataField("centerTexturePath")]
    public string? CenterTexturePath = null;
}

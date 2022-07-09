namespace Content.Client.Atmos.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class FireVisualsComponent : Component
{
    [DataField("fireStackAlternateState")]
    public int FireStackAlternateState = 3;

    [DataField("normalState")]
    public string? NormalState;

    [DataField("alternateState")]
    public string? AlternateState;

    [DataField("sprite")]
    public string? Sprite;
}

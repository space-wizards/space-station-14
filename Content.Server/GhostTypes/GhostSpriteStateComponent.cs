namespace Content.Server.GhostTypes;

[RegisterComponent]
public sealed partial class GhostSpriteStateComponent : Component
{
    [DataField]
    public string Prefix;
}

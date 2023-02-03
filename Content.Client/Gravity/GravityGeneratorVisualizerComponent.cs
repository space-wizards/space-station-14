using Content.Shared.Gravity;

namespace Content.Client.Gravity;

[RegisterComponent]
public sealed class GravityGeneratorVisualizerComponent : Component
{
    [DataField("spritemap")]
    [Access(typeof(GravityGeneratorVisualizerSystem))]
    public Dictionary<GravityGeneratorStatus, string> SpriteMap = new();
}

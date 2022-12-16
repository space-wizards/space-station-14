using JetBrains.Annotations;

namespace Content.Client.Chemistry.Visualizers.Vapor;

[UsedImplicitly]
[RegisterComponent]
public sealed class VaporVisualsComponent : Component
{
    [DataField ("animationTime")]
    public float AnimationTime = 0.25f;

    [DataField ("animationState")]
    public string AnimationState = "chempuff";
}

public enum VaporVisualLayers : byte
{
    Base
}

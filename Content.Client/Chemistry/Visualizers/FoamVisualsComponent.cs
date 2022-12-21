using JetBrains.Annotations;

namespace Content.Client.Chemistry.Visualizers;

[UsedImplicitly]
[RegisterComponent]
public sealed class FoamVisualsComponent : Component
{
    [DataField ("animationTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationTime = 0.25f;
}

public enum FoamLayers : byte
{
    Base,
    Animation
}

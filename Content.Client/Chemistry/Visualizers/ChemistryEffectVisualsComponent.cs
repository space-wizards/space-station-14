using JetBrains.Annotations;

namespace Content.Client.Chemistry.Visualizers;

[UsedImplicitly]
[RegisterComponent]
public sealed class ChemistryEffectVisualsComponent : Component
{
    [DataField ("animationTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationTime = 0.25f;

    [DataField ("animateOnShutdown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AnimateOnShutdown = false;
}

public enum ChemistryEffectLayers : byte
{
    Base,
    Animation
}

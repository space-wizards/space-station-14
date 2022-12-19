using JetBrains.Annotations;

namespace Content.Client.Chemistry.Visualizers;

[UsedImplicitly]
[RegisterComponent]
public sealed class ChemistryEffectVisualsComponent : Component
{
    [DataField ("animationTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationTime = 0.25f;
}

public enum ChemistryEffectLayers : byte
{
    Base,
    Animation
}

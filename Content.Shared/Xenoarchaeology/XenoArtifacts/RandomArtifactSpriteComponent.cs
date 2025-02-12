namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed partial class RandomArtifactSpriteComponent : Component
{
    [DataField]
    public int MinSprite = 1;

    [DataField]
    public int MaxSprite = 14;

    [DataField]
    public double ActivationTime = 2.0;

    public TimeSpan? ActivationStart;
}

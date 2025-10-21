namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed partial class RandomArtifactSpriteComponent : Component
{
    [DataField("minSprite")]
    public int MinSprite = 1;

    [DataField("maxSprite")]
    public int MaxSprite = 14;

    [DataField("activationTime")]
    public double ActivationTime = 0.4;

    public TimeSpan? ActivationStart;
}

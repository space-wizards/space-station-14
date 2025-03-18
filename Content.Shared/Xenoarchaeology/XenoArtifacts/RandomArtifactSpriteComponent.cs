namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[RegisterComponent]
public sealed partial class RandomArtifactSpriteComponent : Component
{
    [DataField("eldritchSprites")]
    public int[] EldritchSprites = [];

    [DataField("martianSprites")]
    public int[] MartianSprites = [];

    [DataField("precursorSprites")]
    public int[] PrecursorSprites = [];

    [DataField("siliconSprites")]
    public int[] SilicionSprites = [];

    [DataField("wizardSprites")]
    public int[] WizardSprites = [];

    [DataField("activationTime")]
    public double ActivationTime = 2.0;

    public TimeSpan? ActivationStart;
}

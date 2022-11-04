namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

[RegisterComponent]
public sealed class ArtifactMagnetTriggerComponent : Component
{
    /// <summary>
    /// how close to the magnet do you have to be?
    /// </summary>
    [DataField("range")]
    public float Range = 40f;
}

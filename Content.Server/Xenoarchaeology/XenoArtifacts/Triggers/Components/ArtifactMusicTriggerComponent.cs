namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when an instrument is played nearby
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactMusicTriggerComponent : Component
{
    /// <summary>
    /// how close does the artifact have to be to the instrument to activate
    /// </summary>
    [DataField("range")]
    public float Range = 5;
}

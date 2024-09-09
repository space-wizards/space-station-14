namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when a nearby entity is resurrected
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactResurrectionTriggerComponent : Component
{
    /// <summary>
    /// How close to the resurrection the artifact has to be for it to trigger.
    /// </summary>
    [DataField("range")]
    public float Range = 15f;
}

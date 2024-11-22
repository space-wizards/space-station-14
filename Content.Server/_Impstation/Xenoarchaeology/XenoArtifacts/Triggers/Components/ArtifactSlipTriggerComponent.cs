namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when a nearby entity is slipped
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactSlipTriggerComponent : Component
{
    /// <summary>
    /// How close to the slip the artifact has to be for it to trigger.
    /// </summary>
    [DataField("range")]
    public float Range = 6f;
}

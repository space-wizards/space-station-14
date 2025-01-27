namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when a nearby entity is stunned
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactStunTriggerComponent : Component
{
    /// <summary>
    /// How close to the stun the artifact has to be for it to trigger.
    /// </summary>
    [DataField("range")]
    public float Range = 6f;
}

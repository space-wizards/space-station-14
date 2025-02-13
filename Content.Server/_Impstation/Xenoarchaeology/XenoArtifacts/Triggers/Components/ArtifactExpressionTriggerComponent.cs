namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// Triggers when a nearby entity emotes
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactExpressionTriggerComponent : Component
{
    /// <summary>
    /// How close to the emote event the artifact has to be for it to trigger.
    /// </summary>
    [DataField("range")]
    public float Range = 2f;
}

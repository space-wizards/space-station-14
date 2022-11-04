namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class ArtifactPressureTriggerComponent : Component
{
    [DataField("minPressureThreshold")]
    public float? MinPressureThreshold;

    [DataField("maxPressureThreshold")]
    public float? MaxPressureThreshold;
}

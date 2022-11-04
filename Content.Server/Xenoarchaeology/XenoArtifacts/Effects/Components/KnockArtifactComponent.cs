namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// This is used for using the "knock" spell when the artifact is activated
/// </summary>
[RegisterComponent]
public sealed class KnockArtifactComponent : Component
{
    [DataField("knockRange")]
    public float KnockRange = 4f;
}

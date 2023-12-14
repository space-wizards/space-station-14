namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// This is used for using the "knock" spell when the artifact is activated
/// </summary>
[RegisterComponent]
public sealed partial class KnockArtifactComponent : Component
{
    /// <summary>
    /// The range of the spell
    /// </summary>
    [DataField("knockRange")]
    public float KnockRange = 4f;
}

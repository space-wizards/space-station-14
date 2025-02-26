namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for using the "knock" spell when the artifact is activated
/// </summary>
[RegisterComponent, Access(typeof(XAEKnockSystem))]
public sealed partial class XAEKnockComponent : Component
{
    /// <summary>
    /// The range of the spell
    /// </summary>
    [DataField]
    public float KnockRange = 4f;
}

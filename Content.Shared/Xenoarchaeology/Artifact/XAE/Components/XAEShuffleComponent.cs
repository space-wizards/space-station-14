namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// When activated, will shuffle the position of all players
/// within a certain radius.
/// </summary>
[RegisterComponent, Access(typeof(XAEShuffleSystem))]
public sealed partial class XAEShuffleComponent : Component
{
    [DataField]
    public float Radius = 7.5f;
}

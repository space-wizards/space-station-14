namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// When activated, will shuffle the position of all players
/// within a certain radius.
/// </summary>
[RegisterComponent]
public sealed partial class ShuffleArtifactComponent : Component
{
    [DataField("radius")]
    public float Radius = 7.5f;
}

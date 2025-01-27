namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Animates a number of entities within a range for a duration. Animated objects attack nearby beings.
/// </summary>
[RegisterComponent]
public sealed partial class AnimateArtifactComponent : Component
{
    /// <summary>
    /// Distance from the artifact to animate objects
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 6f;

    /// <summary>
    /// Duration of the animation.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 15f;

    /// <summary>
    /// Number of objects to animate
    /// </summary>
    [DataField("count"), ViewVariables(VVAccess.ReadWrite)]
    public int Count = 1;
}

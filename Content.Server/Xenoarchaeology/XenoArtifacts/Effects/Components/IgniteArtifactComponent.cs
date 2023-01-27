namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteArtifactComponent : Component
{
    [DataField("range")]
    public float Range = 2f;

    [DataField("minFireStack")]
    public int MinFireStack = 2;

    [DataField("maxFireStack")]
    public int MaxFireStack = 5;
}

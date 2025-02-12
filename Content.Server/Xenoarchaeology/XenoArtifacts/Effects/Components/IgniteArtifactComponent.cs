namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteArtifactComponent : Component
{
    [DataField]
    public float Range = 2f;

    [DataField]
    public int MinFireStack = 2;

    [DataField]
    public int MaxFireStack = 5;
}

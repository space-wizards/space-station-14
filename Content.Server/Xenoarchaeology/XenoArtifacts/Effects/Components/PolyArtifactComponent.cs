
namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class PolyArtifactComponent : Component
{
    [DataField("range")]
    public float Range = 3f;
}

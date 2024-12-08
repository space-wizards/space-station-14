namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent, Access(typeof(XAEIgniteSystem))]
public sealed partial class XAEIgniteComponent : Component
{
    [DataField("range")]
    public float Range = 2f;

    [DataField("minFireStack")]
    public int MinFireStack = 2;

    [DataField("maxFireStack")]
    public int MaxFireStack = 5;
}

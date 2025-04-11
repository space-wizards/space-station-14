namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Artifact that ignites surrounding entities when triggered.
/// </summary>
[RegisterComponent, Access(typeof(XAEIgniteSystem))]
public sealed partial class XAEIgniteComponent : Component
{
    /// <summary>
    /// Range, inside which all entities going be set on fire.
    /// </summary>
    [DataField("range")]
    public float Range = 2f;

    /// <summary>
    /// Min value of fire stacks to apply.
    /// Actual value will be randomized between <see cref="MinFireStack"/>
    /// and <see cref="MaxFireStack"/> for each entity.
    /// </summary>
    [DataField("minFireStack")]
    public int MinFireStack = 2;

    /// <summary>
    /// Max value of fire stacks to apply.
    /// Actual value will be randomized between <see cref="MinFireStack"/>
    /// and <see cref="MaxFireStack"/> for each entity.
    /// </summary>
    [DataField("maxFireStack")]
    public int MaxFireStack = 5;
}

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Knocksdown everything within range, or on the entire local grid.
/// </summary>
[RegisterComponent]
public sealed partial class KnockdownArtifactComponent : Component
{
    /// <summary>
    /// How close do you have to be to get knocked down?
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 8f;

    /// <summary>
    /// How strongly does stuff get thrown?
    /// </summary>
    [DataField("entireGrid"), ViewVariables(VVAccess.ReadWrite)]
    public bool EntireGrid = false;

    /// <summary>
    /// How long to remain knocked down for?
    /// </summary>
    [DataField("knockdownTime"), ViewVariables(VVAccess.ReadWrite)]
    public float KnockdownTime = 3f;
}

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Knocksdown everything within range, or on the entire local grid.
/// </summary>
[RegisterComponent]
public sealed partial class StunOnTriggerComponent : Component
{
    /// <summary>
    /// How close do you have to be to get knocked down?
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 8f;

    /// <summary>
    /// Do we knockdown locally (using range) or all stunnable entities on grid?
    /// </summary>
    [DataField("entireGrid"), ViewVariables(VVAccess.ReadWrite)]
    public bool EntireGrid = false;

    /// <summary>
    /// How long to remain knocked down for?
    /// </summary>
    [DataField("knockdownTime"), ViewVariables(VVAccess.ReadWrite)]
    public float KnockdownTime = 3f;
}

using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated by having a certain amount of gas around it.
/// </summary>
[RegisterComponent, Access(typeof(XATGasSystem))]
public sealed partial class XATGasComponent : Component
{
    /// <summary>
    /// The gas that needs to be supplied
    /// </summary>
    [DataField]
    public Gas TargetGas;

    /// <summary>
    /// The amount of gas needed.
    /// </summary>
    [DataField]
    public float Moles = Atmospherics.MolesCellStandard * 0.1f;

    /// <summary>
    /// Whether or not the gas should be present or not
    /// </summary>
    [DataField]
    public bool ShouldBePresent = true;
}

using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     Change atmospherics temperature until it reach target.
/// </summary>
[RegisterComponent, Access(typeof(XAETemperatureSystem))]
public sealed partial class XAETemperatureComponent : Component
{
    [DataField("targetTemp"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = Atmospherics.T0C;

    [DataField("spawnTemp")]
    public float SpawnTemperature = 100;

    /// <summary>
    ///     If true, artifact will heat/cool not only its current tile, but surrounding tiles too.
    ///     This will change room temperature much faster.
    /// </summary>
    [DataField("affectAdjacent")]
    public bool AffectAdjacentTiles = true;
}

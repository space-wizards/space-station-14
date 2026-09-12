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
    /// Probability by which adjacent tile will be affected by temp change too. Rolled once per each tile adjacent to artifact.
    /// </summary>
    [DataField]
    public float AdjacentTileEffectProbability = 0.5f;
}

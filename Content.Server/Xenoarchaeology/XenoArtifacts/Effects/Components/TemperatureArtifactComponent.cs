using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     Change atmospherics temperature until it reach target.
/// </summary>
[RegisterComponent]
public class TemperatureArtifactComponent : Component
{
    public override string Name => "TemperatureArtifact";

    [DataField("targetTemp")]
    public float TargetTemperature = Atmospherics.T0C;

    [DataField("spawnTemp")]
    public float SpawnTemperature = 100;

    [DataField("maxTempDif")]
    public float MaxTemperatureDifference = 1;
}

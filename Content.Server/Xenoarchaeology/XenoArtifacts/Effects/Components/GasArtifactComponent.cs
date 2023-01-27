using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     Spawn a random gas with random temperature when artifact activated.
/// </summary>
[RegisterComponent]
public sealed partial class GasArtifactComponent : Component
{
    /// <summary>
    ///     Gas that will be spawned when artifact activated.
    ///     If null it will be picked on startup from <see cref="PossibleGases"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("spawnGas")]
    public Gas? SpawnGas;

    /// <summary>
    ///     List of possible activation gases to pick on startup.
    /// </summary>
    [DataField("possibleGas")]
    public List<Gas> PossibleGases = new()
    {
        Gas.Oxygen,
        Gas.Plasma,
        Gas.Nitrogen,
        Gas.CarbonDioxide,
        Gas.Tritium,
        Gas.Miasma,
        Gas.NitrousOxide,
        Gas.Frezon
    };

    /// <summary>
    ///     Temperature of spawned gas. If null it will be picked on startup from range from
    ///     <see cref="MinRandomTemperature"/> to <see cref="MaxRandomTemperature"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("spawnTemperature")]
    public float? SpawnTemperature;

    [DataField("minRandomTemp")]
    public float MinRandomTemperature = 100;

    [DataField("maxRandomTemp")]
    public float MaxRandomTemperature = 400;

    /// <summary>
    ///     Max allowed external atmospheric pressure.
    ///     Artifact will stop spawn gas.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxExternalPressure")]
    public float MaxExternalPressure = Atmospherics.GasMinerDefaultMaxExternalPressure;

    /// <summary>
    ///     Moles of gas to spawn each time when artifact activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("spawnAmount")]
    public float SpawnAmount = Atmospherics.MolesCellStandard * 3;
}

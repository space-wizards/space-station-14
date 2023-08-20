using Content.Shared.Atmos;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Makes a generator emit a gas into the atmosphere when running.
/// </summary>
/// <remarks>
/// The amount of gas produced is linear with the amount of fuel used.
/// </remarks>
/// <seealso cref="SharedGeneratorSystem"/>
/// <seealso cref="FuelGeneratorComponent"/>
[RegisterComponent]
public sealed partial class GeneratorExhaustGasComponent : Component
{
    /// <summary>
    /// The type of gas that will be emitted by the generator.
    /// </summary>
    [DataField("gasType"), ViewVariables(VVAccess.ReadWrite)]
    public Gas GasType = Gas.CarbonDioxide;

    /// <summary>
    /// The amount of moles of gas that should be produced when one unit of fuel is burned.
    /// </summary>
    [DataField("moleRatio"), ViewVariables(VVAccess.ReadWrite)]
    public float MoleRatio = 1;

    /// <summary>
    /// The temperature of created gas.
    /// </summary>
    [DataField("temperature"), ViewVariables(VVAccess.ReadWrite)]
    public float Temperature = Atmospherics.T0C + 100;
}

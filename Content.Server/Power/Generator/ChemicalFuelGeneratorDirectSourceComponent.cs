namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for stuff that can directly be shoved into a generator.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed class ChemicalFuelGeneratorDirectSourceComponent : Component
{
    /// <summary>
    /// The solution to pull fuel material from.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Solution = default!;
}

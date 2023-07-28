namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for stuff that can directly be shoved into a generator.
/// </summary>
[RegisterComponent]
public sealed class ChemicalFuelGeneratorDirectSourceComponent : Component
{
    /// <summary>
    /// The solution to pull fuel material from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}

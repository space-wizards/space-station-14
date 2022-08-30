namespace Content.Server._00OuterRim.Generator;

/// <summary>
/// This is used for stuff that can directly be shoved into a generator.
/// </summary>
[RegisterComponent]
public sealed class ChemicalFuelGeneratorDirectSourceComponent : Component
{
    /// <summary>
    /// Solution name that can added to easily.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}

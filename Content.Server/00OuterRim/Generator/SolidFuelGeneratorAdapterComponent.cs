namespace Content.Server._00OuterRim.Generator;

/// <summary>
/// This is used for allowing you to insert fuel into gens.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed class SolidFuelGeneratorAdapterComponent : Component
{
    [DataField("fuelMaterial"), ViewVariables(VVAccess.ReadWrite)]
    public string FuelMaterial = "Plasma";
}

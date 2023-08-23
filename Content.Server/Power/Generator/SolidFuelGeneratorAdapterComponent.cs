using Content.Shared.Materials;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for allowing you to insert fuel into gens.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed partial class SolidFuelGeneratorAdapterComponent : Component
{
    /// <summary>
    /// The material to accept as fuel.
    /// </summary>
    [DataField("fuelMaterial", customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string FuelMaterial = "Plasma";

    /// <summary>
    /// How much fuel that material should count for.
    /// </summary>
    [DataField("multiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 1.0f;
}

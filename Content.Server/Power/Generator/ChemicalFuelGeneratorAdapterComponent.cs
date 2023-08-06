using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for chemical fuel input into generators.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed class ChemicalFuelGeneratorAdapterComponent : Component
{
    /// <summary>
    /// The acceptable list of input entities.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The conversion factor for different chems you can put in.
    /// </summary>
    [DataField("chemConversionFactors", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, ReagentPrototype>))]
    public Dictionary<string, float> ChemConversionFactors = default!;
}

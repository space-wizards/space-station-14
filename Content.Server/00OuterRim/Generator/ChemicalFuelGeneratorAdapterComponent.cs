using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server._00OuterRim.Generator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed class ChemicalFuelGeneratorAdapterComponent : Component
{
    [DataField("whitelist")] public EntityWhitelist? Whitelist = default!;

    [DataField("chemConversionFactors", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, ReagentPrototype>))]
    public Dictionary<string, float> ChemConversionFactors = default!;
}

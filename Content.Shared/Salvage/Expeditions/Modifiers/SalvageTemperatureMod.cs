using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageTemperatureMod")]
public sealed class SalvageTemperatureMod : IPrototype, IBiomeSpecificMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    /// <inheritdoc/>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    /// <inheritdoc/>
    [DataField("biomes", customTypeSerializer: typeof(PrototypeIdListSerializer<SalvageBiomeMod>))]
    public List<string>? Biomes { get; } = null;

    /// <summary>
    /// Temperature in the planets air mix.
    /// </summary>
    [DataField("temperature")]
    public float Temperature = 293.15f;
}

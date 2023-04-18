using Content.Shared.Parallax.Biomes;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageWeatherMod")]
public sealed class SalvageWeatherMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; } = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    [DataField("weather", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<WeatherPrototype>))]
    public string WeatherPrototype = string.Empty;

    /// <summary>
    /// Whitelist for biomes. If empty assumed any allowed.
    /// </summary>
    [DataField("biomes", customTypeSerializer:typeof(PrototypeIdListSerializer<BiomeTemplatePrototype>))]
    public List<string> Biomes = new();
}

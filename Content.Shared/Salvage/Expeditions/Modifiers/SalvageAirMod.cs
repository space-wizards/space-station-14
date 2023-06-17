using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

/// <summary>
/// Prototype for a planet's air gas mixture.
/// Used when creating the planet for a salvage expedition.
/// Which one is selected depends on the mission difficulty, different weightedRandoms are picked from.
/// </summary>
[Prototype("salvageAirMod")]
public sealed class SalvageAirMod : IPrototype, IBiomeSpecificMod
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc/>
    [DataField("desc")]
    public string Description { get; } = string.Empty;

    /// <inheritdoc/>
    [DataField("cost")]
    public float Cost { get; } = 0f;

    /// <inheritdoc/>
    [DataField("biomes", customTypeSerializer: typeof(PrototypeIdListSerializer<SalvageBiomeMod>))]
    public List<string>? Biomes { get; } = null;

    /// <summary>
    /// Set to true if this planet will have no atmosphere.
    /// </summary>
    [DataField("space")]
    public bool Space;

    /// <summary>
    /// Number of moles of each gas in the mixture.
    /// </summary>
    [DataField("gases")]
    public float[] Gases = new float[Atmospherics.AdjustedNumberOfGases];
}

using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Metabolism.Prototypes;

[Prototype]
public sealed partial class MetabolismTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Reagents that are required for a metabolic reaction to take place
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> RequiredReagents = new();

    /// <summary>
    /// Reagent byproducts created by metabolic reactions
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> WasteReagents = new();

    /// <summary>
    /// Gases that should be absorbed into the bloodstream
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, GasMetabolismData> AbsorbedGases = new();

    /// <summary>
    /// Gases that should be scrubbed into the lung gasmixture and exhaled.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, GasMetabolismData> WasteGases = new();

    /// <summary>
    /// Reagent used to transport energy in the bloodstream. If this is null, energy will not be used in metabolism.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? EnergyReagent = null;

    /// <summary>
    /// What concentration should we try to keep the energy reagent at
    /// </summary>
    [DataField]
    public float TargetEnergyReagentConcentration = 0;

    /// <summary>
    /// How many KiloCalories are there in each reagent unit of the Energy Reagent
    /// </summary>
    [DataField]
    public float KCalPerEnergyReagent = 0;

    /// <summary>
    /// What type of damage should be applied when metabolism fails.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier DeprivationDamage = new();
}

[DataRecord, Serializable, NetSerializable]
public record struct GasMetabolismData(float LowThreshold = 0.95f, float HighThreshold = 1f);

using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
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

    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> RequiredReagents = new();

    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> WasteReagents = new();

    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, GasMetabolismData> AbsorbedGases = new();

    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, GasMetabolismData> WasteGases = new();
}

[DataRecord, Serializable, NetSerializable]
public record struct GasMetabolismData(float LowThreshold = 0.95f, float HighThreshold = 1f);

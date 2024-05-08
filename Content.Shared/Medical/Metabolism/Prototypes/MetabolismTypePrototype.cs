using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

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
    public Dictionary<ProtoId<GasPrototype>, float> AbsorbedGases = new();

    [DataField(required: true)]
    public Dictionary<ProtoId<GasPrototype>, float> WasteGases = new();
}

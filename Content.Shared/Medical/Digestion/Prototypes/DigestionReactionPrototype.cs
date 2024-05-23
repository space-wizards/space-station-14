using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Digestion.Prototypes;

[Prototype]
public sealed partial class DigestionReactionPrototype : IPrototype
{
    // Placeholder implementation of rate limited reactions for digestion because I don't want to rewrite all of chemistry (yet)
    // TODO: once rate limited reactions are implemented, remove this and port functionality to use those instead.

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float DigestionRate = 1;

    [DataField(required: true)]
    public List<(ProtoId<ReagentPrototype>, FixedPoint2)> Reactants = new();

    [DataField]
    public List<(ProtoId<ReagentPrototype>, FixedPoint2)>? Catalysts = null;

    [DataField(required: true)]
    public List<(ProtoId<ReagentPrototype>, FixedPoint2)> Products = new();

}

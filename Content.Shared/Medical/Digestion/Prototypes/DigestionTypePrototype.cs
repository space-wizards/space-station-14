using Content.Shared.Chemistry.Reaction.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Digestion.Prototypes;

[Prototype]
public sealed partial class DigestionTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ProtoId<RateReactionPrototype>> DigestionReactions = new();
}

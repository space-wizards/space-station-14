using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Digestion.Prototypes;

[Prototype]
public sealed partial class DigestionTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ProtoId<ReagentPrototype>? DissolvingReagent = null;

    [DataField]
    public float DissolverConcentration = 0;

    [DataField]
    public List<ProtoId<DigestionReactionPrototype>> DigestionReactions = new();
}

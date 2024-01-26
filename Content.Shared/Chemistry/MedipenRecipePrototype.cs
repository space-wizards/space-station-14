using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry;

[Serializable, Prototype("medipenRecipe")]
public sealed partial class MedipenRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("reagents", required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> RequiredReagents = new();
}

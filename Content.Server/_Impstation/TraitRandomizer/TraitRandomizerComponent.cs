using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.TraitRandomizer;

[RegisterComponent]
public sealed partial class TraitRandomizerComponent : Component
{
    [DataField(required: true)]
    public int MaxTraits;

    [DataField(required: true)]
    public int MinTraits;

    [DataField(required: true)]
    public List<ProtoId<TraitCategoryPrototype>> Categories;
}

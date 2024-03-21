using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mind.Components;

/// <summary>
///     Stores data on all studied recipes of this mind
/// </summary>
[RegisterComponent, Access(typeof(SharedRecipeUnlockSystem))]
public sealed partial class MindLearnedRecipesComponent : Component
{
    [DataField]
    public List<ProtoId<ConstructionPrototype>> LearnedRecipes = new();
}

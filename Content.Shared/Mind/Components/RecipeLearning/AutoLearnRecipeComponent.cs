using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mind.Components;

/// <summary>
///    Automatically teaches the mind of this entity the specified recipes
/// </summary>
[RegisterComponent, Access(typeof(SharedRecipeUnlockSystem))]
public sealed partial class AutoLearnRecipesComponent : Component
{
    [DataField]
    public List<ProtoId<ConstructionPrototype>> Recipes = new();
}

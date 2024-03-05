using Content.Shared.Construction.Prototypes;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Components;

/// <summary>
/// The component can teach the user new recipes.
/// </summary>

[RegisterComponent, Access(typeof(SharedRecipeUnlockSystem))]
public sealed partial class PaperRecipeTeacherComponent : Component
{
    [DataField]
    public List<ProtoId<ConstructionPrototype>> Recipes = new();
}

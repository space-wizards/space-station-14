
namespace Content.Shared.Construction.Components;

/// <summary>
/// The component can teach the user new recipes.
/// </summary>

[RegisterComponent, Access(typeof(SharedLearningRecipesSystem))]
public sealed partial class RecipeTeacherComponent : Component
{
    [DataField]
    public List<string> Recipes = new();
}

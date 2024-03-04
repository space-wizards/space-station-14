namespace Content.Shared.Construction.Components;

/// <summary>
///    Automatically teaches the mind of this entity the specified recipes
/// </summary>
[RegisterComponent, Access(typeof(SharedLearningRecipesSystem))]
public sealed partial class AutoLearnRecipesComponent : Component
{
    [DataField]
    public List<string> Recipes = new();
}

namespace Content.Shared.Construction.Components;

/// <summary>
///     Stores data on all studied recipes of this mind
/// </summary>
[RegisterComponent, Access(typeof(SharedLearningRecipesSystem))]
public sealed partial class LearnedRecipesComponent : Component
{
    [DataField]
    public List<string> LearnedRecipes = new();
}

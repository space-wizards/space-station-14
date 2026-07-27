using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
///     A system that caches all available non-secret microwave recipes.
/// </summary>
public sealed partial class RecipeSystem : EntitySystem
{
    /// <summary>
    ///     A list of all recipes available to the recipe manager.
    /// </summary>
    /// <remarks>
    ///     Order matters! The microwave system will use the *first* valid recipe.
    ///     For this reason, this list gets sorted by "complexity" - recipes with more ingredients
    ///     are sorted first. We make the assumption that more complex recipes are more "specific"
    ///     than less complex recipes whose requirements are also fulfilled.
    /// </remarks>
    public List<MicrowaveMealRecipePrototype> Recipes { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        ReloadRecipes();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MicrowaveMealRecipePrototype>())
            ReloadRecipes();
    }

    private void ReloadRecipes()
    {
        Recipes = ProtoMan
            .EnumeratePrototypes<MicrowaveMealRecipePrototype>()
            .Where(x => !x.SecretRecipe)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.Ingredients.Count())
            .ToList();
    }
}

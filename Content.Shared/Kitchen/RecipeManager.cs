using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

/// <summary>
///     A manager that caches all available non-secret microwave recipes.
/// </summary>
public sealed partial class RecipeManager
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    /// <summary>
    ///     A list of all recipes available to the recipe manager.
    /// </summary>
    /// <remarks>
    ///     Order matters! The microwave system will use the *first* valid recipe.
    ///     For this reason, this list gets sorted by "complexity" - recipes with more ingredients
    ///     are sorted first. We make the assumption that more complex recipes are more "specific"
    ///     than less complex recipes whose requirements are also fulfilled.
    /// </remarks>
    public List<FoodRecipePrototype> Recipes { get; private set; } = new();

    /// <summary>
    ///     Caches all recipes and sorts them.
    /// </summary>
    public void Initialize()
    {
        CacheRecipes();

        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        // We need to re-cache recipes here, as recipes may be added or deleted,
        // or ingredient changes may alter the order that recipes are sorted.
        if (args.WasModified<FoodRecipePrototype>())
            CacheRecipes();
    }

    private void CacheRecipes()
    {
        Recipes = new();

        foreach (var item in _prototypeManager.EnumeratePrototypes<FoodRecipePrototype>())
            if (!item.SecretRecipe)
                Recipes.Add(item);

        Recipes.Sort(new RecipeComparer());
    }

    /// <summary>
    /// Check if a prototype ids appears in any of the recipes that exist.
    /// </summary>
    public bool SolidAppears(string solidId)
    {
        return Recipes.Any(recipe => recipe.Ingredients.Solids.ContainsKey(solidId));
    }

    private sealed class RecipeComparer : Comparer<FoodRecipePrototype>
    {
        public override int Compare(FoodRecipePrototype? x, FoodRecipePrototype? y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            var nx = x.Ingredients.Count();
            var ny = y.Ingredients.Count();
            return -nx.CompareTo(ny);
        }
    }
}

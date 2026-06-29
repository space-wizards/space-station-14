
namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Gets a complete ordered list of usable recipes for this appliance.
    /// </summary>
    /// <remarks>
    ///     Note that the order of recipes is meaningful. When a valid recipe is chosen, the first item
    ///     in the list that satisfies the conditions of the recipe is selected.
    ///
    ///     Recipe prototypes in the recipe manager are pre-sorted based on complexity, so more "specific"
    ///     recipes will be selected first. Secret recipes come before all non-secret prototype recipes.
    ///     Do not sort the result of this function!
    /// </remarks>
    /// <param name="uid">The appliance to get recipes for.</param>
    /// <returns>A complete list of usable recipe prototypes.</returns>
    private IReadOnlyList<FoodRecipePrototype> GetAvailableRecipes(EntityUid uid)
    {
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(uid, ref getRecipesEv);

        var recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);

        return recipes;
    }
}

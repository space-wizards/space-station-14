using System.Linq;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Helper function to check if a microwave has ingredient contents.
    /// </summary>
    /// <param name="microwave">The microwave entity.</param>
    /// <returns>Whether or not this microwave contains anything.</returns>
    [PublicAPI]
    public bool HasContents(Entity<MicrowaveComponent?> microwave)
    {
        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return false;

        return microwave.Comp.Storage.ContainedEntities.Any();
    }

    /// <summary>
    ///     Given a recipe, a list of available ingredients, and a cooking time, this functions
    ///     gets how many times we can make this given recipe.
    /// </summary>
    /// <param name="recipe">A cooking recipe.</param>
    /// <param name="ingredients">The ingredients we have available.</param>
    /// <param name="cookTime">How long we are cooking for.</param>
    /// <returns>How many portions of the recipe can be made.</returns>
    [PublicAPI]
    public static uint GetRecipePortions(FoodRecipePrototype recipe,
        CookingIngredients ingredients,
        uint cookTime)
    {
        // Our cooking time must be a multiple of the recipe's cooking time.
        // For example: If a recipe takes 10 seconds to cook, then you can't make it with a 15 second timer.
        // However, if you use a 30 second timer, you could make three of that recipe on one timer.
        if (cookTime % recipe.CookTime != 0)
            return 0;

        // TODO: there's actually a kind of nasty edge case microwave economics issue here,
        // all reagents / materials / solids will be included, but when the recipe is actually made,
        // solids are used first, then materials, then reagents.
        // thus, recipe detection might thing you have "more" ingredients than you actually do.
        //
        // moral of the story: I hate microwaves
        var portionCount = cookTime / recipe.CookTime;
        var ingredientPortions = ingredients.PortionForRecipe(recipe.Ingredients);
        portionCount = Math.Min(portionCount, ingredientPortions);

        return portionCount;
    }

    /// <summary>
    ///     Given a microwave and a list of available ingredients, this function gets the first valid
    ///     usable recipe for cooking.
    /// </summary>
    /// <remarks>
    ///     The microwave entity itself is used to determine cooking time and get secret recipes.
    /// </remarks>
    /// <param name="microwave">The microwave entity.</param>
    /// <param name="ingredients">A list of available ingredients.</param>
    /// <returns>
    ///     The first valid recipe we can use. If there is none, this is (null, 0).
    /// </returns>
    [PublicAPI]
    public (FoodRecipePrototype? recipe, uint count) GetRecipe(Entity<MicrowaveComponent?> microwave, CookingIngredients ingredients)
    {
        (FoodRecipePrototype? recipe, uint count) emptyRecipe = (null, 0);
        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return emptyRecipe;

        var recipes = GetRecipesForMicrowave(microwave.Owner);
        var cookTime = microwave.Comp.CurrentCookTimerTime;
        var recipePortions = recipes.Select(recipe =>
            {
                var portions = GetRecipePortions(recipe, ingredients, cookTime);
                return (recipe, portions);
            });

        return recipePortions.FirstOrNull(r => r.portions > 0)
            ?? emptyRecipe;
    }
}

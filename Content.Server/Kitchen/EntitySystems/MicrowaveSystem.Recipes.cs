using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    /// This event tries to get secret recipes that the microwave might be capable of.
    /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
    /// </summary>
    private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
    {
        foreach (var recipeId in ent.Comp.ProvidedRecipes)
            if (_prototype.Resolve(recipeId, out var recipeProto))
                args.Recipes.Add(recipeProto);
    }

    /// <summary>
    ///     Given a recipe, a list of available ingredietns, and a cooking time, this functions
    ///     gets how many times we can make this given recipe.
    /// </summary>
    /// <param name="recipe">A cooking recipe.</param>
    /// <param name="ingredients">The ingredients we have available.</param>
    /// <param name="cookTime">How long we are cooking for.</param>
    /// <returns>How many portions of the recipe can be made.</returns>
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
    private (FoodRecipePrototype? recipe, uint count) GetRecipe(Entity<MicrowaveComponent> microwave, CookingIngredients ingredients)
    {
        var recipes = GetRecipesForMicrowave(microwave.Owner);
        var cookTime = microwave.Comp.CurrentCookTimerTime;
        var recipePortions = recipes.Select(recipe =>
            {
                var portions = GetRecipePortions(recipe, ingredients, cookTime);
                return (recipe, portions);
            });

        return recipePortions.FirstOrNull(r => r.portions > 0)
            ?? (null, 0);
    }

    /// <summary>
    ///     Gets a complete list of usable recipes for this microwave - including secret recipes and
    ///     all recipe prototypes.
    /// </summary>
    /// <remarks>
    ///     Note that the order of recipes is meaningful. When a valid recipe is chosen, the first item
    ///     in the list that satisfies the conditions of the recipe is selected.
    ///
    ///     Recipe prototypes are pre-sorted based on complexity, so more "specific" recipes will be selected first.
    ///     Secret recipes come before all non-secret prototype recipes. Do not sort this!
    /// </remarks>
    /// <param name="microwave">The microwave entity.</param>
    /// <returns>A complete list of usable recipe prototypes.</returns>
    private List<FoodRecipePrototype> GetRecipesForMicrowave(EntityUid microwave)
    {
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(microwave, ref getRecipesEv);

        var recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);

        return recipes;
    }

    /// <summary>
    ///     Gets all usable ingredients from a given entity and adds it to the list of respective ingredients.
    /// </summary>
    /// <remarks>
    ///     The entity itself is a "solid".
    ///     If it's a stack, then that stack is its "materials".
    ///     If it has a usable ingredient solution, then the solution's contents are "reagents".
    /// </remarks>
    /// <param name="item">The entity to use as ingredients.</param>
    /// <param name="solids">The dictionary of available recipe solids.</param>
    /// <param name="materials">The dictionary of available recipe materials.</param>
    /// <param name="reagents">The dictionary of available recipe reagents.</param>
    private void SumItemIngredients(EntityUid item,
        Dictionary<EntProtoId, int> solids,
        Dictionary<ProtoId<StackPrototype>, int> materials,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> reagents)
    {
        if (TryGetSolidId(item, out var solidId))
        {
            if (!solids.TryAdd(solidId.Value, 1))
                solids[solidId.Value] += 1;
        }

        if (TryGetMaterialId(item, out var materialId, out var stack))
        {
            var count = stack.Value.Comp.Count;
            if (!materials.TryAdd(materialId.Value, count))
                materials[materialId.Value] += count;
        }

        if (TryGetUsableIngredientSolution(item, out var _, out var solution))
        {
            foreach (var (reagent, quantity) in solution.Contents)
            {
                if (!reagents.TryAdd(reagent.Prototype, quantity))
                    reagents[reagent.Prototype] += quantity;
            }
        }
    }

    /// <summary>
    ///     Gets a complete list of recipe-usable ingredients from a list of items, including solids,
    ///     materials, and reagents.
    /// </summary>
    /// <param name="items">The list of items to use as ingredients.</param>
    /// <returns>Cooking ingredient quantities representing the total usable ingredient list.</returns>
    private CookingIngredients GetTotalIngredients(List<EntityUid> items)
    {
        var solids = new Dictionary<EntProtoId, int>();
        var materials = new Dictionary<ProtoId<StackPrototype>, int>();
        var reagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();

        foreach (var item in items)
            SumItemIngredients(item, solids, materials, reagents);

        return new(solids, materials, reagents);
    }

    /// <summary>
    ///     Attempts to get a solution from an entity that can be used as viable ingredients in a recipe.
    /// </summary>
    /// <remarks>
    ///     For example, a beaker's contents will work, but not the contents of an uncracked egg.
    /// </remarks>
    /// <param name="uid">The entity to attempt to get a usable ingredient solution for.</param>
    /// <param name="solutionEntity">A usable solution entity, if available.</param>
    /// <param name="solution">A usable solution, if available.</param>
    /// <returns>Whether or not a usable ingredient solution was successfully retrieved.</returns>
    private bool TryGetUsableIngredientSolution(EntityUid uid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out Solution? solution)
    {
        // Have to break the eggs before we can use them!
        return _solutionContainer.TryGetDrainableSolution(uid, out solutionEntity, out solution);
    }

    /// <summary>
    ///     Given a dictionary of materials that need to be spent in a recipe, and the amount of stacks
    ///     of a material we have available, this function gets the number of stacks we need to remove
    ///     from the stack entity. It also removes this amount from the remaining materials dictionary.
    /// </summary>
    /// <param name="availableStacks">How many stacks we have available.</param>
    /// <param name="stackId">The material ID associated with the stack.</param>
    /// <param name="remainingMaterials">How many material stacks still need to be spent in the recipe.</param>
    /// <returns>How many stacks we should remove from the available stack entity.</returns>
    private static int SpendMaterialQuantity(int availableStacks,
        ProtoId<StackPrototype> stackId,
        Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        var remaining = remainingMaterials[stackId];
        var spent = Math.Min(availableStacks, remaining);
        remaining -= spent;

        if (remaining == 0)
            remainingMaterials.Remove(stackId);
        else
            remainingMaterials[stackId] = remaining;

        return spent;
    }

    /// <summary>
    ///     Given a dictionary of reagents that need to be spent in a recipe, and a quantity of a reagent
    ///     that we have available in a solution, this function gets the amount of reagents we need to
    ///     remove from the solution. This also removes that amount from the "reagents to spend" dictionary.
    /// </summary>
    /// <param name="recipeQuantity">How much reagent we have available.</param>
    /// <param name="reagent">The ID of the reagent we are spending.</param>
    /// <param name="remainingReagents">A dictionary of reagents that still need to be spent.</param>
    /// <returns>How much we should reduce the available reagent volume by.</returns>
    private static FixedPoint2 SpendReagentQuantity(FixedPoint2 recipeQuantity,
        ProtoId<ReagentPrototype> reagent,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        var remaining = remainingReagents[reagent];
        var spent = FixedPoint2.Min(recipeQuantity, remaining);
        remaining -= spent;

        if (remaining == FixedPoint2.Zero)
            remainingReagents.Remove(reagent);
        else
            remainingReagents[reagent] = remaining;

        return spent;
    }

    /// <summary>
    ///     Removes a solid ingredient that is used in a recipe, removing it from a dictionary of
    ///     remaining solids that still need to be spent in the recipe.
    /// </summary>
    /// <param name="item">The entity to remove.</param>
    /// <param name="itemProto">The solid ID of the ingredient.</param>
    /// <param name="container">The microwave's ingredient storage container.</param>
    /// <param name="remainingSolids">A dictionary of recipe solids that still need to be spent.</param>
    private void SubtractSolidContents(EntityUid item,
        EntProtoId itemProto,
        Container container,
        Dictionary<EntProtoId, int> remainingSolids)
    {
        if (!remainingSolids.ContainsKey(itemProto))
            return;

        remainingSolids[itemProto] -= 1;
        if (remainingSolids[itemProto] <= 0)
            remainingSolids.Remove(itemProto);

        _container.Remove(item, container);
        QueueDel(item);
    }

    /// <summary>
    ///     Given a dictionary of remaining material stacks that need to be spent in a recipe, this function
    ///     reduces a stack entity's count by however many stacks need to be spent. This also removes the
    ///     material stack count from the remaining materials dictionary.
    /// </summary>
    /// <remarks>
    ///     If a recipe calls for two stacks of plasma sheets, and you put 5 sheets in the microwave, then
    ///     the plasma sheet stack would reduce by 2 (leaving you with 3 remaining), and plasma will
    ///     be removed from the remaining materials dictionary.
    /// </remarks>
    /// <param name="ent">The stack entity.</param>
    /// <param name="remainingMaterials">A dictionary of recipe materials that still need to be spent.</param>
    private void SubtractMaterialContents(Entity<StackComponent> ent,
        Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var recipeQuantity = SpendMaterialQuantity(startingQuantity, stackId, remainingMaterials);

        _stack.ReduceCount(ent.AsNullable(), recipeQuantity);
    }

    /// <summary>
    ///     Given a dictionary of remaining reagents that still need to be spent in a recipe, this function iterates
    ///     over a solution's contents and subtracts reagents according to what remains to be spent. This also removes
    ///     it from the "remaining reagents to spend" dictionary accordingly.
    /// </summary>
    /// <remarks>
    ///     Say you still have 6u mayonnaise remaining, and the solution has 10u mayonnaise, then 6u of
    ///     mayonnaise would be removed from the solution, leaving you with 4u left over. Then, mayonnaise
    ///     is removed from the dictionary of remaining reagents.
    /// </remarks>
    /// <param name="solutionEntity">The solution entity.</param>
    /// <param name="solution">The solution itself.</param>
    /// <param name="remainingReagents">A dictionary of reagents that still need to be spent.</param>
    /// <returns>A new dictionary of how many reagents still need to be spent.</returns>
    private Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> SubtractReagentContents(
        Entity<SolutionComponent> solutionEntity,
        Solution solution,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        var newReagentsRemaining = remainingReagents.ShallowClone();

        foreach (var (reagent, _) in remainingReagents)
        {
            var startingQuantity = solution.GetTotalPrototypeQuantity(reagent);
            if (startingQuantity == 0)
                continue;

            var recipeQuantity = SpendReagentQuantity(startingQuantity, reagent, newReagentsRemaining);
            _solutionContainer.RemoveReagent(solutionEntity, reagent, recipeQuantity);
        }

        return newReagentsRemaining;
    }

    /// <summary>
    ///     Attempt to get the solid ID of a given entity.
    /// </summary>
    /// <param name="item">The entity to retrieve a solid ID for.</param>
    /// <param name="solidId">The solid ID of the entity, if any.</param>
    /// <returns>
    ///     Whether or not the solid ID was successfully retrieved. False if entity lacks an entity prototype ID.
    /// </returns>
    // TODO: Solids should be tag-based, or something like that. Not prototype-based.
    private bool TryGetSolidId(EntityUid item,
        [NotNullWhen(true)] out EntProtoId? solidId)
    {
        solidId = MetaData(item).EntityPrototype?.ID;
        return solidId != null;
    }

    /// <summary>
    ///     Attempt to get the material stack ID of a given entity.
    /// </summary>
    /// <param name="item">The entity to retrieve a stack ID for</param>
    /// <param name="material">The stack prototype associated with this entity, if any.</param>
    /// <param name="stackEnt">This entity represented as an entity with StackComponent, if feasible.</param>
    /// <returns>
    ///     Whether or not a material ID is successfully retrieved. False if this entity is not a stack.
    /// </returns>
    private bool TryGetMaterialId(EntityUid item,
        [NotNullWhen(true)] out ProtoId<StackPrototype>? material,
        [NotNullWhen(true)] out Entity<StackComponent>? stackEnt)
    {
        material = null;
        stackEnt = null;

        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        material = stack.StackTypeId;
        stackEnt = (item, stack);

        return material != null && stackEnt != null;
    }

    /// <summary>
    ///     Spend ingredients in the microwave based on a given recipe. This deletes ingredient entities (solids),
    ///     subtracts respective stack counts (materials), and removes reagents from ingredient containers (reagents).
    /// </summary>
    /// <remarks>
    ///     This function does not check whether or not the contents have *enough* ingredients to
    ///     subtract - it simply performs the subtraction.
    /// </remarks>
    /// <param name="component">The microwave component.</param>
    /// <param name="recipe">The recipe used to spend ingredients.</param>
    /// <param name="count">How many times this recipe is spent in ingredient volumes.</param>
    private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe, uint count = 1)
    {
        var portioned = recipe.Ingredients * count;
        var remainingSolids = portioned.Solids;
        var remainingMaterials = portioned.Materials;
        var remainingReagents = portioned.Reagents;
        var microwaveItems = component.Storage.ContainedEntities.ToArray();

        foreach (var item in microwaveItems)
        {
            if (remainingSolids.Count > 0
                && TryGetSolidId(item, out var solidId)
                && remainingSolids.ContainsKey(solidId.Value))
            {
                SubtractSolidContents(item, solidId.Value, component.Storage, remainingSolids);
                continue;
                // We're exiting early here; if the solid ingredient is removed from the container,
                // then we shouldn't be attempting to use its material stack or reagents.
            }

            if (remainingMaterials.Count > 0
                && TryGetMaterialId(item, out var materialId, out var stack)
                && remainingMaterials.ContainsKey(materialId.Value))
            {
                SubtractMaterialContents(stack.Value, remainingMaterials);
                if (Deleted(stack) || stack.Value.Comp.Count <= 0)
                    continue;
                // We're exiting early here - if the stack is empty, then the stack entity
                // is gonna be deleted. Which means we shouldn't be using its reagents.
            }

            if (remainingReagents.Count > 0
                && TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution)
                && solution.Volume > 0)
                remainingReagents = SubtractReagentContents(solutionEntity.Value,
                    solution,
                    remainingReagents);
        }
    }
}

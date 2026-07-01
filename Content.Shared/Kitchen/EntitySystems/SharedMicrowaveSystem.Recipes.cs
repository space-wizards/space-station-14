using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// This event tries to get secret recipes that the microwave might be capable of.
    /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
    /// </summary>
    private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
    {
        foreach (var recipeId in ent.Comp.ProvidedRecipes)
            if (ProtoMan.Resolve(recipeId, out var recipeProto))
                args.Recipes.Add(recipeProto);
    }

    /// <summary>
    ///     Adds all usable ingredients from a given entity to an ingredient list.
    /// </summary>
    /// <remarks>
    ///     The entity itself is a "solid".
    ///     If it's a stack, then that stack is its "materials".
    ///     If it has a usable ingredient solution, then the solution's contents are "reagents".
    /// </remarks>
    /// <param name="item">The entity to use as ingredients.</param>
    /// <param name="ingredients">A dictionary of available ingredients to add to.</param>
    private void AddItemIngredients(EntityUid item, ref CookingIngredients ingredients)
    {
        if (TryGetSolidId(item, out var solidId))
            ingredients.AddSolid(solidId.Value);

        if (TryGetMaterialId(item, out var materialId, out var stack))
            ingredients.AddMaterial(materialId.Value, stack.Value.Comp.Count);

        if (TryGetUsableIngredientSolution(item, out var _, out var solution))
            foreach (var (reagent, quantity) in solution.Contents)
                ingredients.AddReagent(reagent.Prototype, quantity);
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
        return SolutionSys.TryGetDrainableSolution(uid, out solutionEntity, out solution);
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
        CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Materials.TryGetValue(stackId, out var remaining))
            return 0;

        var spent = Math.Min(availableStacks, remaining);
        ingredientsToSpend.AddMaterial(stackId, -spent);

        return spent;
    }

    /// <summary>
    ///     Given a dictionary of reagents that need to be spent in a recipe, and a quantity of a reagent
    ///     that we have available in a solution, this function gets the amount of reagents we need to
    ///     remove from the solution. This also removes that amount from the "reagents to spend" dictionary.
    /// </summary>
    /// <param name="availableQuantity">How much reagent we have available.</param>
    /// <param name="reagent">The ID of the reagent we are spending.</param>
    /// <param name="remainingReagents">A dictionary of reagents that still need to be spent.</param>
    /// <returns>How much we should reduce the available reagent volume by.</returns>
    private static FixedPoint2 SpendReagentQuantity(FixedPoint2 availableQuantity,
        ProtoId<ReagentPrototype> reagent,
        CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Reagents.TryGetValue(reagent, out var remaining))
            return 0;

        var spent = FixedPoint2.Min(availableQuantity, remaining);
        ingredientsToSpend.AddReagent(reagent, -spent);

        return spent;
    }

    /// <summary>
    ///     Removes a solid ingredient that is used in a recipe, removing it from the dictionary of
    ///     remaining solids that still need to be spent in the recipe.
    /// </summary>
    /// <param name="item">The entity to remove.</param>
    /// <param name="itemProto">The solid ID of the ingredient.</param>
    /// <param name="container">The microwave's ingredient storage container.</param>
    /// <param name="ingredientsToSpend">The struct representing ingredients we still need to spend.</param>
    private void SubtractSolidContents(EntityUid item,
        EntProtoId itemProto,
        Container container,
        CookingIngredients ingredientsToSpend)
    {
        if (!ingredientsToSpend.Solids.ContainsKey(itemProto))
            return;

        ingredientsToSpend.AddSolid(itemProto, -1);
        ContainerSys.Remove(item, container);
        PredictedQueueDel(item);
    }

    /// <summary>
    ///     Given a dictionary of remaining material stacks that need to be spent in a recipe, this function
    ///     reduces a stack entity's count by however many stacks need to be spent. This also removes the
    ///     material stack count from our remaining ingredients.
    /// </summary>
    /// <remarks>
    ///     If a recipe calls for two stacks of plasma sheets, and you put 5 sheets in the microwave, then
    ///     the plasma sheet stack would reduce by 2 (leaving you with 3 remaining), and plasma will
    ///     be removed from the remaining materials dictionary.
    /// </remarks>
    /// <param name="ent">The stack entity.</param>
    /// <param name="ingredientsToSpend">The struct representing ingredients we still need to spend.</param>
    private void SubtractMaterialContents(Entity<StackComponent> ent,
        CookingIngredients ingredientsToSpend)
    {
        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var quantityToRemove = SpendMaterialQuantity(startingQuantity, stackId, ingredientsToSpend);

        _stack.ReduceCount(ent.AsNullable(), quantityToRemove);
    }

    /// <summary>
    ///     Given a dictionary of remaining reagents that still need to be spent in a recipe, this function iterates
    ///     over a solution's contents and subtracts reagents according to the reagents to spend. This also removes
    ///     it from our remaining ingredients.
    /// </summary>
    /// <remarks>
    ///     Say you still have 6u mayonnaise remaining, and the solution has 10u mayonnaise, then 6u of
    ///     mayonnaise would be removed from the solution, leaving you with 4u left over. Then, mayonnaise
    ///     is removed from the dictionary of remaining reagents.
    /// </remarks>
    /// <param name="solutionEntity">The solution entity.</param>
    /// <param name="solution">The solution itself.</param>
    /// <param name="ingredientsToSpend">The struct representing ingredients we still need to spend.</param>
    private void SubtractReagentContents(Entity<SolutionComponent> solutionEntity,
        Solution solution,
        CookingIngredients ingredientsToSpend)
    {
        var reagentsToProcess = ingredientsToSpend.Reagents.Keys.ToList();

        foreach (var reagent in reagentsToProcess)
        {
            var availableQuantity = solution.GetTotalPrototypeQuantity(reagent);
            if (availableQuantity == 0)
                continue;

            var quantityToRemove = SpendReagentQuantity(availableQuantity, reagent, ingredientsToSpend);
            SolutionSys.RemoveReagent(solutionEntity, reagent, quantityToRemove);
        }
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
        var ingredientsToSpend = recipe.Ingredients * count;
        var solidsToSpend = ingredientsToSpend.Solids;
        var materialsToSpend = ingredientsToSpend.Materials;
        var reagentsToSpend = ingredientsToSpend.Reagents;
        var microwaveItems = component.Storage.ContainedEntities.ToArray();

        foreach (var item in microwaveItems)
        {
            if (solidsToSpend.Count > 0
                && TryGetSolidId(item, out var solidId)
                && solidsToSpend.ContainsKey(solidId.Value))
            {
                SubtractSolidContents(item, solidId.Value, component.Storage, ingredientsToSpend);
                continue;
                // We're exiting early here; if the solid ingredient is removed from the container,
                // then we shouldn't be attempting to use its material stack or reagents.
            }

            if (materialsToSpend.Count > 0
                && TryGetMaterialId(item, out var materialId, out var stack)
                && materialsToSpend.ContainsKey(materialId.Value))
            {
                SubtractMaterialContents(stack.Value, ingredientsToSpend);
                if (Deleted(stack) || stack.Value.Comp.Count <= 0)
                    continue;
                // We're exiting early here - if the stack is empty, then the stack entity
                // is gonna be deleted. Which means we shouldn't be using its reagents.
            }

            if (reagentsToSpend.Count > 0
                && TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution)
                && solution.Volume > 0)
                SubtractReagentContents(solutionEntity.Value, solution, ingredientsToSpend);
        }
    }
}

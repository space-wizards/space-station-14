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

    private List<FoodRecipePrototype> GetRecipesForMicrowave(EntityUid microwave)
    {
        var getRecipesEv = new GetSecretRecipesEvent();
        RaiseLocalEvent(microwave, ref getRecipesEv);

        var recipes = getRecipesEv.Recipes;
        recipes.AddRange(_recipeManager.Recipes);

        return recipes;
    }

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

    private CookingIngredients GetTotalIngredients(Entity<MicrowaveComponent> microwave, List<EntityUid> items)
    {
        var solids = new Dictionary<EntProtoId, int>();
        var materials = new Dictionary<ProtoId<StackPrototype>, int>();
        var reagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();

        foreach (var item in items)
            SumItemIngredients(item, solids, materials, reagents);

        return new(solids, materials, reagents);
    }

    private bool TryGetUsableIngredientSolution(EntityUid uid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out Solution? solution)
    {
        // Have to break the eggs before we can use them!
        return _solutionContainer.TryGetDrainableSolution(uid, out solutionEntity, out solution);
    }

    private static int SpendMaterialQuantity(int recipeQuantity,
        ProtoId<StackPrototype> stackId,
        Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        var remaining = remainingMaterials[stackId];
        var spent = Math.Min(recipeQuantity, remaining);
        remaining -= spent;

        if (remaining == 0)
            remainingMaterials.Remove(stackId);
        else
            remainingMaterials[stackId] = remaining;

        return spent;
    }

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

    private void SubtractSolidContents(EntityUid item,
        EntProtoId itemProto,
        Container container,
        Dictionary<EntProtoId, int> remainingSolids)
    {
        remainingSolids[itemProto] -= 1;
        if (remainingSolids[itemProto] <= 0)
            remainingSolids.Remove(itemProto);

        _container.Remove(item, container);
        QueueDel(item);
    }

    private void SubtractMaterialContents(Entity<StackComponent> ent,
        Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var recipeQuantity = SpendMaterialQuantity(startingQuantity, stackId, remainingMaterials);

        _stack.ReduceCount(ent.AsNullable(), recipeQuantity);
    }

    private void TrySubtractReagentContents(Entity<SolutionComponent> solutionEntity,
        Solution solution,
        FoodRecipePrototype recipe,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        foreach (var (reagent, _) in recipe.Ingredients.Reagents)
        {
            if (!remainingReagents.ContainsKey(reagent))
                continue;

            var startingQuantity = solution.GetTotalPrototypeQuantity(reagent);
            var recipeQuantity = SpendReagentQuantity(startingQuantity, reagent, remainingReagents);
            _solutionContainer.RemoveReagent(solutionEntity, reagent, recipeQuantity);
        }
    }

    private bool TryGetSolidId(EntityUid item,
        [NotNullWhen(true)] out EntProtoId? solidId)
    {
        solidId = MetaData(item).EntityPrototype?.ID;
        return solidId != null;
    }

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

    private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe, uint count = 1)
    {
        var portioned = recipe.Ingredients * count;
        var remainingSolids = portioned.Solids.ShallowClone();
        var remainingMaterials = portioned.Materials.ShallowClone();
        var remainingReagents = portioned.Reagents.ShallowClone();

        foreach (var item in component.Storage.ContainedEntities)
        {
            if (TryGetSolidId(item, out var solidId) && remainingSolids.ContainsKey(solidId.Value))
            {
                SubtractSolidContents(item, solidId.Value, component.Storage, remainingSolids);
                continue;
                // We're exiting early here; if the solid ingredient is removed from the container,
                // then we shouldn't be attempting to use its material stack or reagents.
            }

            if (TryGetMaterialId(item, out var materialId, out var stack)
                && remainingMaterials.ContainsKey(materialId.Value))
            {
                SubtractMaterialContents(stack.Value, remainingMaterials);
                if (stack.Value.Comp.Count <= 0)
                    continue;
                // We're exiting early here - if the stack is empty, then the stack entity
                // is gonna be deleted. Which means we shouldn't be using its reagents.
            }

            if (TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution))
                TrySubtractReagentContents(solutionEntity.Value,
                    solution,
                    recipe,
                    remainingReagents);
        }
    }
}

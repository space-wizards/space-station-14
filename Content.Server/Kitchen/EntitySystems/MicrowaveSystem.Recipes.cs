using System.Diagnostics.CodeAnalysis;
using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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

    public static (FoodRecipePrototype, int) CanSatisfyRecipe(MicrowaveComponent component,
        FoodRecipePrototype recipe,
        Dictionary<string, int> solids,
        Dictionary<string, FixedPoint2> reagents)
    {
        var portions = 0;

        if (component.CurrentCookTimerTime % recipe.CookTime != 0)
        {
            //can't be a multiple of this recipe
            return (recipe, 0);
        }

        foreach (var solid in recipe.Solids)
        {
            if (!solids.ContainsKey(solid.Key))
                return (recipe, 0);

            if (solids[solid.Key] < solid.Value)
                return (recipe, 0);

            portions = portions == 0
                ? solids[solid.Key] / solid.Value
                : Math.Min(portions, solids[solid.Key] / solid.Value);
        }

        foreach (var reagent in recipe.Reagents)
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]
            if (!reagents.ContainsKey(reagent.Key))
                return (recipe, 0);

            if (reagents[reagent.Key] < reagent.Value)
                return (recipe, 0);

            portions = portions == 0
                ? reagents[reagent.Key].Int() / reagent.Value.Int()
                : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
        }

        //cook only as many of those portions as time allows
        return (recipe, (int)Math.Min(portions, component.CurrentCookTimerTime / recipe.CookTime));
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

    private void SubtractMaterialContents(Entity<StackComponent?> ent,
        Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
            return;

        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var recipeQuantity = SpendMaterialQuantity(startingQuantity, stackId, remainingMaterials);

        _stack.ReduceCount(ent, recipeQuantity);
    }

    private void TrySubtractReagentContents(Entity<SolutionComponent> solutionEntity,
        Solution solution,
        FoodRecipePrototype recipe,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        foreach (var (reagent, _) in recipe.Reagents)
        {
            if (!remainingReagents.ContainsKey(reagent))
                continue;

            var startingQuantity = solution.GetTotalPrototypeQuantity(reagent);
            var recipeQuantity = SpendReagentQuantity(startingQuantity, reagent, remainingReagents);
            _solutionContainer.RemoveReagent(solutionEntity, reagent, recipeQuantity);
        }
    }

    private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe)
    {
        var remainingSolids = new Dictionary<EntProtoId, int>(recipe.Solids);
        var remainingMaterials = new Dictionary<ProtoId<StackPrototype>, int>(recipe.Materials);
        var remainingReagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(recipe.Reagents);

        foreach (var item in component.Storage.ContainedEntities)
        {
            var itemProto = MetaData(item).EntityPrototype?.ID;
            if (itemProto != null && remainingSolids.ContainsKey(itemProto))
            {
                SubtractSolidContents(item, itemProto, component.Storage, remainingSolids);
                continue;
                // We're exiting early here; if the solid ingredient is removed from the container,
                // then we shouldn't be attempting to use its material stack or reagents.
            }

            if (TryComp<StackComponent>(item, out var stack) && remainingMaterials.ContainsKey(stack.StackTypeId))
            {
                SubtractMaterialContents((item, stack), remainingMaterials);
                if (stack.Count <= 0)
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

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

    private bool TryGetUsableIngredientSolution(EntityUid uid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out Solution? solution)
    {
        // Have to break the eggs before we can use them!
        return _solutionContainer.TryGetDrainableSolution(uid, out solutionEntity, out solution);
    }

    private static FixedPoint2 GetReagentSubtractionQuantity(FixedPoint2 recipeQuantity,
        ProtoId<ReagentPrototype> reagent,
        ref Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        if (recipeQuantity >= remainingReagents[reagent])
        {
            recipeQuantity = remainingReagents[reagent];
            remainingReagents.Remove(reagent);
        }
        else
            remainingReagents[reagent] -= recipeQuantity;

        return recipeQuantity;
    }

    private static int GetMaterialSubtractionQuantity(int recipeQuantity,
        ProtoId<StackPrototype> stackId,
        ref Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        if (recipeQuantity >= remainingMaterials[stackId])
        {
            recipeQuantity = remainingMaterials[stackId];
            remainingMaterials.Remove(stackId);
        }
        else
            remainingMaterials[stackId] -= recipeQuantity;

        return recipeQuantity;
    }

    private void TrySubtractReagentContents(Entity<SolutionComponent> solutionEntity,
        Solution solution,
        FoodRecipePrototype recipe,
        ref Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        foreach (var (reagent, _) in recipe.Reagents)
        {
            if (!remainingReagents.ContainsKey(reagent))
                continue;

            var startingQuantity = solution.GetTotalPrototypeQuantity(reagent);
            var recipeQuantity = GetReagentSubtractionQuantity(startingQuantity, reagent, ref remainingReagents);
            _solutionContainer.RemoveReagent(solutionEntity, reagent, recipeQuantity);
        }
    }

    private void SubtractSolidContents(EntityUid item,
        EntProtoId itemProto,
        Container container,
        ref Dictionary<EntProtoId, int> remainingSolids)
    {
        remainingSolids[itemProto] -= 1;
        if (remainingSolids[itemProto] <= 0)
            remainingSolids.Remove(itemProto);

        _container.Remove(item, container);
        QueueDel(item);
    }
    private void SubtractMaterialContents(Entity<StackComponent?> ent,
        ref Dictionary<ProtoId<StackPrototype>, int> remainingMaterials)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
            return;

        var stack = ent.Comp;
        var stackId = stack.StackTypeId;
        var startingQuantity = stack.Count;
        var recipeQuantity = GetMaterialSubtractionQuantity(startingQuantity, stackId, ref remainingMaterials);

        _stack.ReduceCount(ent, recipeQuantity);
    }

    private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe)
    {
        var remainingSolids = new Dictionary<EntProtoId, int>(recipe.Solids);
        var remainingMaterials = new Dictionary<ProtoId<StackPrototype>, int>(recipe.Materials);
        var remainingReagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(recipe.Reagents);

        foreach (var item in component.Storage.ContainedEntities)
        {
            var itemProto = MetaData(item).EntityPrototype;
            if (itemProto != null && remainingSolids.ContainsKey(itemProto))
            {
                SubtractSolidContents(item, itemProto, component.Storage, ref remainingSolids);
                continue;
                // We're exiting early here; if the solid ingredient is removed from the container,
                // then we shouldn't be attempting to use its material stack or reagents.
            }

            if (TryComp<StackComponent>(item, out var stack) && remainingMaterials.ContainsKey(stack.StackTypeId))
            {
                SubtractMaterialContents((item, stack), ref remainingMaterials);
                if (stack.Count <= 0)
                    continue;
                // We're exiting early here - if the stack is empty, then the stack entity
                // is gonna be deleted. Which means we shouldn't be using its reagents.
            }

            if (TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution))
                TrySubtractReagentContents(solutionEntity.Value,
                    solution,
                    recipe,
                    ref remainingReagents);
        }
    }
}

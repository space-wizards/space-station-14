using System.Diagnostics.CodeAnalysis;
using Content.Server.Kitchen.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Stacks;
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

    private static FixedPoint2 GetSubtractionQuantity(FixedPoint2 recipeQuantity,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents,
        ProtoId<ReagentPrototype> reagent)
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

    private void SubtractReagentContents(EntityUid item,
        FoodRecipePrototype recipe,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> remainingReagents)
    {
        if (!TryGetUsableIngredientSolution(item, out var solutionEntity, out var solution))
            return;

        foreach (var (reagent, _) in recipe.Reagents)
        {
            if (!remainingReagents.ContainsKey(reagent))
                continue;

            var startingQuantity = solution.GetTotalPrototypeQuantity(reagent);
            var recipeQuantity = GetSubtractionQuantity(startingQuantity, remainingReagents, reagent);
            _solutionContainer.RemoveReagent(solutionEntity.Value, reagent, recipeQuantity);
        }
    }

    private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe)
    {
        var remainingReagents = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>(recipe.Reagents);

        foreach (var item in component.Storage.ContainedEntities)
        {
            SubtractReagentContents(item, recipe, remainingReagents);
        }

        foreach (var (ingredientId, count) in recipe.Solids)
        {
            for (var i = 0; i < count; i++)
            {
                foreach (var item in component.Storage.ContainedEntities)
                {
                    string? itemID = null;

                    if (TryComp<StackComponent>(item, out var stackComp))
                        itemID = _prototype.Index(stackComp.StackTypeId).Spawn;
                    else
                    {
                        var metaData = MetaData(item);
                        if (metaData.EntityPrototype == null)
                            continue;

                        itemID = metaData.EntityPrototype.ID;
                    }

                    if (itemID != ingredientId)
                        continue;

                    if (stackComp is not null)
                    {
                        if (stackComp.Count == 1)
                        {
                            _container.Remove(item, component.Storage);
                        }
                        _stack.ReduceCount((item, stackComp), 1);
                        break;
                    }
                    else
                    {
                        _container.Remove(item, component.Storage);
                        Del(item);
                        break;
                    }
                }
            }
        }
    }
}

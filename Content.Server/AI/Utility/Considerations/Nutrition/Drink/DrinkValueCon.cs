using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Solution.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Drink
{
    public sealed class DrinkValueCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null
                || target.Deleted
                || !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(target, "drink", out var drink))
            {
                return 0.0f;
            }

            var nutritionValue = 0;

            foreach (var reagent in drink.Contents)
            {
                // TODO
                nutritionValue += (reagent.Quantity * 30).Int();
            }

            return nutritionValue / 1000.0f;
        }
    }
}

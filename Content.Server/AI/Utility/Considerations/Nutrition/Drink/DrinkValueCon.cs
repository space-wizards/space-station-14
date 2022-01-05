using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Drink
{
    public sealed class DrinkValueCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null
                || IoCManager.Resolve<IEntityManager>().Deleted(target)
                || !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(target.Value, DrinkComponent.DefaultSolutionName, out var drink))
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

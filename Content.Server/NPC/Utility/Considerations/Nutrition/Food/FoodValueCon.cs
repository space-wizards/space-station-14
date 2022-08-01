using Content.Server.Chemistry.EntitySystems;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.Nutrition.Components;

namespace Content.Server.NPC.Utility.Considerations.Nutrition.Food
{
    public sealed class FoodValueCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent<FoodComponent?>(target.Value, out var foodComp)
                               || !EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(target.Value, foodComp.SolutionName, out var food))
            {
                return 0.0f;
            }

            var nutritionValue = 0;

            foreach (var reagent in food.Contents)
            {
                // TODO
                nutritionValue += (reagent.Quantity * 30).Int();
            }

            return nutritionValue / 1000.0f;
        }
    }
}

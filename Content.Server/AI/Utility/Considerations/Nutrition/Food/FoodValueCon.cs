using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Chemistry;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Food
{
    public sealed class FoodValueCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (!target.TryGetComponent(out SolutionContainerComponent food))
            {
                return 0.0f;
            }

            var nutritionValue = 0;

            foreach (var reagent in food.ReagentList)
            {
                // TODO
                nutritionValue += (reagent.Quantity * 30).Int();
            }

            return nutritionValue / 1000.0f;
        }
    }
}

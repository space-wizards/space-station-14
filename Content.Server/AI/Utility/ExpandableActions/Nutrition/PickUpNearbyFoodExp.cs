using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Nutrition.Food;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Nutrition.Food;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Nutrition;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.ExpandableActions.Nutrition
{
    public sealed class PickUpNearbyFoodExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();
            return new[]
            {
                considerationsManager.Get<HungerCon>().PresetCurve(context, PresetCurve.Nutrition),
                considerationsManager.Get<FreeHandCon>().BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyFoodState>().GetValue())
            {
                yield return new PickUpFood {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}

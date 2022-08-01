using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Nutrition.Food;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Hands;
using Content.Server.NPC.Utility.Considerations.Nutrition.Food;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Nutrition;

namespace Content.Server.NPC.Utility.ExpandableActions.Nutrition
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

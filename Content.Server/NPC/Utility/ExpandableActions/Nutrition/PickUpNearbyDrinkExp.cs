using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Nutrition.Drink;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Hands;
using Content.Server.NPC.Utility.Considerations.Nutrition.Drink;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Nutrition;

namespace Content.Server.NPC.Utility.ExpandableActions.Nutrition
{
    public sealed class PickUpNearbyDrinkExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();
            return new[]
            {
                considerationsManager.Get<ThirstCon>().PresetCurve(context, PresetCurve.Nutrition),
                considerationsManager.Get<FreeHandCon>().BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyDrinkState>().GetValue())
            {
                yield return new PickUpDrink() {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}

using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Nutrition.Food;
using Content.Server.AI.Utility.ExpandableActions;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Nutrition;

namespace Content.Server.Nyanotrasen.AI.Utitlity.ExpandableActions.Nutrition
{
    public sealed class EatFoliageExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<HungerCon>()
                    .BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyFoliageState>().GetValue())
            {
                yield return new Actions.Nutrition.Food.EatFoliage() { Owner = owner, Target = entity, Bonus = Bonus };
            }
        }
    }
}

using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Nutrition.Drink;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Nutrition.Drink;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Inventory;
using Content.Server.Nutrition.Components;

namespace Content.Server.NPC.Utility.ExpandableActions.Nutrition
{
    public sealed class UseDrinkInInventoryExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NeedsBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();
            return new[]
            {
                considerationsManager.Get<ThirstCon>().PresetCurve(context, PresetCurve.Nutrition)
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (!IoCManager.Resolve<IEntityManager>().HasComponent<DrinkComponent>(entity))
                {
                    continue;
                }

                yield return new UseDrinkInInventory {Owner = owner, Target = entity, Bonus = Bonus};
            }
        }
    }
}

using Content.Server.Clothing.Components;
using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.Utility.Actions.Clothing.Gloves;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Clothing;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Clothing;
using Content.Shared.Inventory;

namespace Content.Server.NPC.Utility.ExpandableActions.Clothing.Gloves
{
    public sealed class PickUpAnyNearbyGlovesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NormalBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();
            return new[]
            {
                considerationsManager.Get<ClothingInSlotCon>().Slot("gloves", context)
                    .InverseBoolCurve(context),
                considerationsManager.Get<ClothingInInventoryCon>().Slot(SlotFlags.GLOVES, context)
                    .InverseBoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyClothingState>().GetValue())
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ClothingComponent? clothing) &&
                    (clothing.Slots & SlotFlags.GLOVES) != 0)
                {
                    yield return new PickUpGloves {Owner = owner, Target = entity, Bonus = Bonus};
                }
            }
        }
    }
}

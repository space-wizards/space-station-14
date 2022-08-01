using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Inventory;
using Content.Server.NPC.Operators.Nutrition;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Inventory;
using Content.Server.NPC.Utility.Considerations.Nutrition.Drink;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;

namespace Content.Server.NPC.Utility.Actions.Nutrition.Drink
{
    public sealed class UseDrinkInInventory : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, Target),
                new UseDrinkInInventoryOperator(Owner, Target),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<TargetInOurInventoryCon>()
                    .BoolCurve(context),
                considerationsManager.Get<DrinkValueCon>()
                    .QuadraticCurve(context, 1.0f, 0.4f, 0.0f, 0.0f),
            };
        }
    }
}

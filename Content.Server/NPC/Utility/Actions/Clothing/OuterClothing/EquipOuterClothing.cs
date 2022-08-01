using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Inventory;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Inventory;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;

namespace Content.Server.NPC.Utility.Actions.Clothing.OuterClothing
{
    public sealed class EquipOuterClothing : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, Target),
                new UseItemInInventoryOperator(Owner, Target),
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
                considerationsManager.Get<CanPutTargetInInventoryCon>()
                    .BoolCurve(context),
            };
        }
    }
}

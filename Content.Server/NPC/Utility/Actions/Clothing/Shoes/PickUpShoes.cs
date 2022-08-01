using Content.Server.NPC.Operators.Sequences;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.Utility.Considerations.Inventory;
using Content.Server.NPC.Utility.Considerations.Movement;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;

namespace Content.Server.NPC.Utility.Actions.Clothing.Shoes
{
    public sealed class PickUpShoes : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new GoPickupEntitySequence(Owner, Target).Sequence;
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
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}

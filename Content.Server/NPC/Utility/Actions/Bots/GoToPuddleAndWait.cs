using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Generic;
using Content.Server.NPC.Operators.Movement;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.ActionBlocker;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Movement;

namespace Content.Server.NPC.Utility.Actions.Bots
{
    public sealed class GoToPuddleAndWait : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator = new MoveToEntityOperator(Owner, Target, 0, 0);
            float waitTime = 3f;

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new WaitOperator(waitTime),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
            context.GetState<MoveTargetState>().SetValue(Target);
            // Can just set ourselves as entity given unarmed just inherits from meleeweapon
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanMoveCon>()
                    .BoolCurve(context),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}

using Content.Server.NPC.Considerations.Bots;
using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Bots;
using Content.Server.NPC.Operators.Generic;
using Content.Server.NPC.Operators.Movement;
using Content.Server.NPC.Operators.Speech;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.ActionBlocker;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Movement;

namespace Content.Server.NPC.Utility.Actions.Bots
{
    public sealed class InjectNearby : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator = new MoveToEntityOperator(Owner, Target);
            float waitTime = 3f;

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new SpeakOperator(Owner, Loc.GetString("medibot-start-inject")),
                new WaitOperator(waitTime),
                new InjectOperator(Owner, Target),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
            context.GetState<MoveTargetState>().SetValue(Target);
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
                considerationsManager.Get<CanInjectCon>()
                    .BoolCurve(context),
            };
        }
    }
}

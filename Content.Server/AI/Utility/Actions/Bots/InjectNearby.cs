using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Generic;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Operators.Bots;
using Content.Server.AI.Operators.Speech;
using Content.Server.AI.WorldState;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.ActionBlocker;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.Utility.Considerations.Bot;


namespace Content.Server.AI.Utility.Actions.Bots
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

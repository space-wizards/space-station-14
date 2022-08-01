using Content.Server.NPC.Operators;
using Content.Server.NPC.Operators.Inventory;
using Content.Server.NPC.Operators.Movement;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.Utility.Considerations.Containers;
using Content.Server.NPC.Utility.Considerations.Movement;
using Content.Server.NPC.Utility.Considerations.State;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.NPC.WorldState.States.Inventory;

namespace Content.Server.NPC.Utility.Actions.Idle
{
    /// <summary>
    /// If we just picked up a bunch of stuff and have time then close it
    /// </summary>
    public sealed class CloseLastEntityStorage : UtilityAction
    {
        public override float Bonus => IdleBonus + 0.01f;

        public override void SetupOperators(Blackboard context)
        {
            var lastStorage = context.GetState<LastOpenedStorageState>().GetValue();

            if (!lastStorage.IsValid())
            {
                ActionOperators = new Queue<AiOperator>(new AiOperator[]
                {
                    new CloseLastStorageOperator(Owner),
                });

                return;
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(Owner, lastStorage),
                new CloseLastStorageOperator(Owner),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            var lastStorage = context.GetState<LastOpenedStorageState>();
            context.GetState<TargetEntityState>().SetValue(lastStorage.GetValue());
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<StoredStateEntityIsNullCon>().Set(typeof(LastOpenedStorageState), context)
                    .InverseBoolCurve(context),
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
				considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }

    }
}

using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Considerations.State;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Idle
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

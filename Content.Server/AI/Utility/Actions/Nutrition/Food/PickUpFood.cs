using System;
using System.Collections.Generic;
using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Considerations.Nutrition.Food;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Nutrition.Food
{
    public sealed class PickUpFood : UtilityAction
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
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<FoodValueCon>()
                    .QuadraticCurve(context, 1.0f, 0.4f, 0.0f, 0.0f),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}

using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Considerations.Nutrition.Drink;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Nutrition.Drink
{
    public sealed class PickUpDrink : UtilityAction
    {
        private IEntity _entity;

        public PickUpDrink(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new GoPickupEntitySequence(Owner, _entity).Sequence;
        }

        protected override Consideration[] Considerations => new Consideration[]
        {
            new TargetAccessibleCon(
                new BoolCurve()),
            new FreeHandCon(
                new BoolCurve()),
            new ThirstCon(
                new LogisticCurve(1000f, 1.3f, -1.0f, 0.5f)),
            new DistanceCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.02f, 0.0f)),
            new DrinkValueCon(
                new QuadraticCurve(1.0f, 0.4f, 0.0f, 0.0f)),
        };

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
        }
    }
}

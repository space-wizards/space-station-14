using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Ranged;
using Content.Server.AI.Utility.Considerations.Combat.Ranged.Ballistic;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Ranged.Ballistic
{
    public sealed class PickUpBallisticMagWeapon : UtilityAction
    {
        private IEntity _entity;

        public PickUpBallisticMagWeapon(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new GoPickupEntitySequence(Owner, _entity).Sequence;
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<MoveTargetState>().SetValue(_entity);
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<WeaponEntityState>().SetValue(_entity);
        }

        protected override Consideration[] Considerations { get; } = {
            new HeldRangedWeaponsCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.0f, 0.0f)),
            new TargetAccessibleCon(
                new BoolCurve()),
            new FreeHandCon(
                new BoolCurve()),
            // For now don't grab empty guns - at least until we can start storing stuff in inventory
            new BallisticAmmoCon(
                new BoolCurve()),
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
            new RangedWeaponFireRateCon(
                new QuadraticCurve(1.0f, 0.5f, 0.0f, 0.0f)),
            // TODO: Ballistic accuracy? Depends how the design transitions
        };
    }
}

using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class PickUpMeleeWeapon : UtilityAction
    {
        private IEntity _entity;

        public PickUpMeleeWeapon(IEntity owner, IEntity entity, float weight) : base(owner)
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
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<WeaponEntityState>().SetValue(_entity);
        }

        protected override Consideration[] Considerations { get; } = {
            new TargetAccessibleCon(
                new BoolCurve()),
            new FreeHandCon(
                new BoolCurve()),
            new HasMeleeWeaponCon(
                new InverseBoolCurve()),
            new DistanceCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.02f, 0.0f)),
            new MeleeWeaponDamageCon(
                new QuadraticCurve(1.0f, 0.25f, 0.0f, 0.0f)),
            new MeleeWeaponSpeedCon(
                new QuadraticCurve(-1.0f, 0.5f, 1.0f, 0.0f)),
        };
    }
}

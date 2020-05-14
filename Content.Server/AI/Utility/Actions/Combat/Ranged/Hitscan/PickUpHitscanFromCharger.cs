using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Combat.Ranged;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Ranged;
using Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Hands;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan
{
    public sealed class PickUpHitscanFromCharger : UtilityAction
    {
        private IEntity _entity;
        private IEntity _charger;

        public PickUpHitscanFromCharger(IEntity owner, IEntity entity, IEntity charger, float weight) : base(owner)
        {
            _entity = entity;
            _charger = charger;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(Owner, _charger),
                new WaitForHitscanChargeOperator(_entity),
                new PickupEntityOperator(Owner, _entity),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<MoveTargetState>().SetValue(_entity);
            context.GetState<TargetEntityState>().SetValue(_entity);
        }

        protected override Consideration[] Considerations { get; } = {
            new HeldRangedWeaponsCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.0f, 0.0f)),
            new TargetAccessibleCon(
                new BoolCurve()),
            new FreeHandCon(
                new BoolCurve()),
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
            // TODO: ChargerHasPower
            new RangedWeaponFireRateCon(
                new QuadraticCurve(1.0f, 0.5f, 0.0f, 0.0f)),
            new HitscanWeaponDamageCon(
                new QuadraticCurve(1.0f, 0.25f, 0.0f, 0.0f)),
        };
    }
}

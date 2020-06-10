using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.GameObjects.Components.Power.Chargers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan
{
    public sealed class PutHitscanInCharger : UtilityAction
    {
        // Maybe a bad idea to not allow override
        public override bool CanOverride => false;
        private readonly IEntity _charger;

        public PutHitscanInCharger(IEntity owner, IEntity charger, float weight) : base(owner)
        {
            _charger = charger;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            var weapon = context.GetState<EquippedEntityState>().GetValue();

            if (weapon == null || _charger.GetComponent<WeaponCapacitorChargerComponent>().HeldItem != null)
            {
                ActionOperators = new Queue<AiOperator>();
                return;
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(Owner, _charger),
                new InteractWithEntityOperator(Owner,  _charger),
                // Separate task will deal with picking it up
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<MoveTargetState>().SetValue(_charger);
            context.GetState<TargetEntityState>().SetValue(_charger);
        }

        protected override Consideration[] Considerations { get; } =
        {
            new HitscanWeaponEquippedCon(
                new BoolCurve()),
            new HitscanChargerFullCon(
                new InverseBoolCurve()),
            new HitscanChargerRateCon(
                new QuadraticCurve(1.0f, 0.5f, 0.0f, 0.0f)),
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
            new HitscanChargeCon(
                new QuadraticCurve(-1.2f, 2.0f, 1.2f, 0.0f)),
        };
    }
}

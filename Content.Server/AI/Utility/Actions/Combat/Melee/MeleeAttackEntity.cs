using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Combat;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class MeleeAttackEntity : UtilityAction
    {
        private IEntity _entity;

        public MeleeAttackEntity(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            var moveOperator = new MoveToEntityOperator(Owner, _entity);
            var equipped = context.GetState<EquippedEntityState>().GetValue();
            if (equipped != null && equipped.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent))
            {
                moveOperator.DesiredRange = meleeWeaponComponent.Range - 0.01f;
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new SwingMeleeWeaponOperator(Owner, _entity),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<MoveTargetState>().SetValue(_entity);
            var equipped = context.GetState<EquippedEntityState>().GetValue();
            context.GetState<WeaponEntityState>().SetValue(equipped);
        }

        protected override Consideration[] Considerations { get; } = {
            // Check if we have a weapon; easy-out
            new MeleeWeaponEquippedCon(
                new BoolCurve()),
            // Don't attack a dead target
            new TargetIsDeadCon(
                new InverseBoolCurve()),
            // Deprioritise a target in crit
            new TargetIsCritCon(
                new QuadraticCurve(-0.8f, 1.0f, 1.0f, 0.0f)),
            // Somewhat prioritise distance
            new DistanceCon(
                new QuadraticCurve(1.0f, 1.0f, 0.02f, 0.0f)),
            // Prefer weaker targets
            new TargetHealthCon(
                new QuadraticCurve(1.0f, 0.4f, 0.0f, -0.02f)),
            new MeleeWeaponSpeedCon(
                new QuadraticCurve(1.0f, 0.5f, 0.0f, 0.0f)),
            new MeleeWeaponDamageCon(
                new QuadraticCurve(1.0f, 0.25f, 0.0f, 0.0f)),
        };
    }
}

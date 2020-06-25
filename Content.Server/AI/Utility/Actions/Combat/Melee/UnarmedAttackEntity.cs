using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Combat.Melee;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class UnarmedAttackEntity : UtilityAction
    {
        private IEntity _entity;

        public UnarmedAttackEntity(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator;
            if (Owner.TryGetComponent(out UnarmedCombatComponent unarmedCombatComponent))
            {
                moveOperator = new MoveToEntityOperator(Owner, _entity, unarmedCombatComponent.Range - 0.01f);
            }
            // I think it's possible for this to happen given planning is time-sliced?
            // TODO: At this point we should abort
            else
            {
                moveOperator = new MoveToEntityOperator(Owner, _entity);
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new UnarmedCombatOperator(Owner, _entity), 
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(_entity);
            context.GetState<MoveTargetState>().SetValue(_entity);
            // Can just set ourselves as entity given unarmed just inherits from meleeweapon
            context.GetState<WeaponEntityState>().SetValue(Owner);
        }

        protected override Consideration[] Considerations { get; } = {
            new CanUnarmedCombatCon(
                new BoolCurve()),
            // Don't attack a dead target
            new TargetIsDeadCon(
                new InverseBoolCurve()),
            // Deprioritise a target in crit
            new TargetIsCritCon(
                new QuadraticCurve(-0.8f, 1.0f, 1.0f, 0.0f)),
            // Somewhat prioritise distance
            new DistanceCon(
                new QuadraticCurve(-1.0f, 1.0f, 1.02f, 0.0f)),
            // Prefer weaker targets
            new TargetHealthCon(
                new QuadraticCurve(1.0f, 0.4f, 0.0f, -0.02f)),
            // TODO: Consider our Speed and Damage to compare this to using a weapon
            // Also need to unequip our weapon if we have one (xenos can't hold one so no issue for now)
        };
    }
}
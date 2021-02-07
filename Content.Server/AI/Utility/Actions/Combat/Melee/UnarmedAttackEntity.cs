using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Combat.Melee;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class UnarmedAttackEntity : UtilityAction
    {
        private readonly IEntity _entity;

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

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<TargetIsDeadCon>()
                    .InverseBoolCurve(context),
                considerationsManager.Get<TargetIsCritCon>()
                    .QuadraticCurve(context, -0.8f, 1.0f, 1.0f, 0.0f),
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<TargetHealthCon>()
                    .PresetCurve(context, PresetCurve.TargetHealth),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
                // TODO: Consider our Speed and Damage to compare this to using a weapon
                // Also need to unequip our weapon if we have one (xenos can't hold one so no issue for now)
            };
        }
    }
}

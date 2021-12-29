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
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class UnarmedAttackEntity : UtilityAction
    {
        public EntityUid Target { get; set; } = default!;

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out UnarmedCombatComponent? unarmedCombatComponent))
            {
                moveOperator = new MoveToEntityOperator(Owner, Target, unarmedCombatComponent.Range - 0.01f);
            }
            // I think it's possible for this to happen given planning is time-sliced?
            // TODO: At this point we should abort
            else
            {
                moveOperator = new MoveToEntityOperator(Owner, Target);
            }

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new UnarmedCombatOperator(Owner, Target),
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<TargetEntityState>().SetValue(Target);
            context.GetState<MoveTargetState>().SetValue(Target);
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

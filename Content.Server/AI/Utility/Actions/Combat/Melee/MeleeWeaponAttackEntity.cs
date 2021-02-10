using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Combat.Melee;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.AI.WorldState.States.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class MeleeWeaponAttackEntity : UtilityAction
    {
        private readonly IEntity _entity;

        public MeleeWeaponAttackEntity(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator;
            var equipped = context.GetState<EquippedEntityState>().GetValue();
            if (equipped != null && equipped.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent))
            {
                moveOperator = new MoveToEntityOperator(Owner, _entity, meleeWeaponComponent.Range - 0.01f);
            }
            else
            {
                // TODO: Abort
                moveOperator = new MoveToEntityOperator(Owner, _entity);
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
                considerationsManager.Get<MeleeWeaponSpeedCon>()
                    .QuadraticCurve(context, 1.0f, 0.5f, 0.0f, 0.0f),
                considerationsManager.Get<MeleeWeaponDamageCon>()
                    .QuadraticCurve(context, 1.0f, 0.25f, 0.0f, 0.0f),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}

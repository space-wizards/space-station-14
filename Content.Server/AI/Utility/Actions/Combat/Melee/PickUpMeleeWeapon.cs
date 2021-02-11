using System;
using System.Collections.Generic;
using Content.Server.AI.Operators.Sequences;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Containers;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class PickUpMeleeWeapon : UtilityAction
    {
        private readonly IEntity _entity;

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

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<TargetDistanceCon>()
                    .PresetCurve(context, PresetCurve.Distance),
                considerationsManager.Get<MeleeWeaponDamageCon>()
                    .QuadraticCurve(context, 1.0f, 0.25f, 0.0f, 0.0f),
                considerationsManager.Get<MeleeWeaponSpeedCon>()
                    .QuadraticCurve(context, -1.0f, 0.5f, 1.0f, 0.0f),
                considerationsManager.Get<TargetAccessibleCon>()
                    .BoolCurve(context),
            };
        }
    }
}

using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utility.Considerations.Inventory;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Combat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Actions.Combat.Melee
{
    public sealed class EquipMelee : UtilityAction
    {
        private readonly IEntity _entity;

        public EquipMelee(IEntity owner, IEntity entity, float weight) : base(owner)
        {
            _entity = entity;
            Bonus = weight;
        }

        public override void SetupOperators(Blackboard context)
        {
            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                new EquipEntityOperator(Owner, _entity)
            });
        }

        protected override void UpdateBlackboard(Blackboard context)
        {
            base.UpdateBlackboard(context);
            context.GetState<WeaponEntityState>().SetValue(_entity);
            context.GetState<TargetEntityState>().SetValue(_entity);
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanPutTargetInInventoryCon>()
                    .BoolCurve(context),
                considerationsManager.Get<MeleeWeaponSpeedCon>()
                    .QuadraticCurve(context, 1.0f, 0.5f, 0.0f, 0.0f),
                considerationsManager.Get<MeleeWeaponDamageCon>()
                    .QuadraticCurve(context, 1.0f, 0.25f, 0.0f, 0.0f),
            };
        }
    }
}

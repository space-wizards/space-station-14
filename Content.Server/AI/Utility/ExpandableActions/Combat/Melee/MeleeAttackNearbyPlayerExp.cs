using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Melee;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.Utils;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Melee
{
    public sealed class MeleeAttackNearbyPlayerExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<MeleeWeaponEquippedCon>()
                    .BoolCurve(context),
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.TryGetComponent(out AiControllerComponent controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var entity in Visibility.GetEntitiesInRange(owner.Transform.GridPosition, typeof(SpeciesComponent),
                controller.VisionRadius))
            {
                if (entity.HasComponent<BasicActorComponent>() && entity != owner)
                {
                    yield return new MeleeWeaponAttackEntity(owner, entity, Bonus);
                }
            }
        }
    }
}

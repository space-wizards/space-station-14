using System;
using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Melee;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Combat.Melee;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Melee
{
    public sealed class MeleeAttackNearbyExp : ExpandableUtilityAction
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out AiControllerComponent? controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var target in EntitySystem.Get<AiFactionTagSystem>()
                .GetNearbyHostiles(owner, controller.VisionRadius))
            {
                yield return new MeleeWeaponAttackEntity {Owner = owner, Target = target, Bonus = Bonus};
            }
        }
    }
}

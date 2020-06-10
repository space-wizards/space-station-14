using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Combat.Ranged.Hitscan;
using Content.Server.AI.Utils;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Power.Chargers;

namespace Content.Server.AI.Utility.ExpandableActions.Combat.Ranged.Hitscan
{
    public sealed class PickUpHitscanFromChargersExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.CombatPrepBonus;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.TryGetComponent(out AiControllerComponent controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var entity in Visibility.GetEntitiesInRange(owner.Transform.GridPosition, typeof(WeaponCapacitorChargerComponent),
                controller.VisionRadius))
            {
                var contained = entity.GetComponent<WeaponCapacitorChargerComponent>().HeldItem;

                if (contained != null)
                {
                    yield return new PickUpHitscanFromCharger(owner, entity, contained, Bonus);
                }
            }
        }
    }
}

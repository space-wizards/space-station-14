using System;
using System.Collections.Generic;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Nutrition.Drink;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Nutrition;
using Content.Server.GameObjects.Components.Movement;

namespace Content.Server.AI.Utility.ExpandableActions.Nutrition
{
    public sealed class PickUpNearbyDrinkExp : ExpandableUtilityAction
    {
        public override BonusWeight Bonus => BonusWeight.Normal;

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            if (!owner.TryGetComponent(out AiControllerComponent controller))
            {
                throw new InvalidOperationException();
            }

            foreach (var entity in context.GetState<NearbyDrinkState>().GetValue())
            {
                yield return new PickUpDrink(owner, entity, Bonus);
            }
        }
    }
}

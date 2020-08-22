#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.AI.Operators.Nutrition
{
    public class UseDrinkInInventoryOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _target;
        private float _interactionCooldown;

        public UseDrinkInInventoryOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_interactionCooldown >= 0)
            {
                _interactionCooldown -= frameTime;
                return Outcome.Continuing;
            }

            // TODO: Also have this check storage a la backpack etc.
            if (_target.Deleted ||
                !_owner.TryGetComponent(out HandsComponent? handsComponent) ||
                !_target.TryGetComponent(out ItemComponent? itemComponent))
            {
                return Outcome.Failed;
            }

            DrinkComponent? drinkComponent = null;

            foreach (var slot in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(slot) != itemComponent) continue;
                handsComponent.ActiveHand = slot;
                if (!_target.TryGetComponent(out drinkComponent))
                {
                    return Outcome.Failed;
                }

                // This should also implicitly open it.
                handsComponent.ActivateItem();
                _interactionCooldown = IoCManager.Resolve<IRobustRandom>().NextFloat() + 0.5f;
            }

            if (drinkComponent == null)
            {
                return Outcome.Failed;
            }

            if (drinkComponent.Deleted ||
                drinkComponent.Empty ||
                _owner.TryGetComponent(out ThirstComponent? thirstComponent) &&
                thirstComponent.CurrentThirst >= thirstComponent.ThirstThresholds[ThirstThreshold.Okay])
            {
                return Outcome.Success;
            }

            return Outcome.Continuing;
        }
    }
}

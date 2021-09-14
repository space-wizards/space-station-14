using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.AI.Operators.Nutrition
{
    public class UseFoodInInventoryOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _target;
        private float _interactionCooldown;

        public UseFoodInInventoryOperator(IEntity owner, IEntity target)
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

            FoodComponent? foodComponent = null;

            foreach (var slot in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(slot) != itemComponent) continue;
                handsComponent.ActiveHand = slot;
                if (!_target.TryGetComponent(out foodComponent))
                {
                    return Outcome.Failed;
                }

                // This should also implicitly open it.
                handsComponent.ActivateItem();
                _interactionCooldown = IoCManager.Resolve<IRobustRandom>().NextFloat() + 0.5f;
            }

            if (foodComponent == null)
            {
                return Outcome.Failed;
            }

            if (_target.Deleted ||
                foodComponent.UsesRemaining == 0 ||
                _owner.TryGetComponent(out HungerComponent? hungerComponent) &&
                hungerComponent.CurrentHunger >= hungerComponent.HungerThresholds[HungerThreshold.Okay])
            {
                return Outcome.Success;
            }

            return Outcome.Continuing;
        }
    }
}

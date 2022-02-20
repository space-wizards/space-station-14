using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.AI.Operators.Nutrition
{
    public sealed class UseFoodInInventoryOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _target;
        private float _interactionCooldown;

        public UseFoodInInventoryOperator(EntityUid owner, EntityUid target)
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

            var entities = IoCManager.Resolve<IEntityManager>();

            // TODO: Also have this check storage a la backpack etc.
            if (entities.Deleted(_target) ||
                !entities.TryGetComponent(_owner, out HandsComponent? handsComponent) ||
                !entities.TryGetComponent(_target, out SharedItemComponent? itemComponent))
            {
                return Outcome.Failed;
            }

            FoodComponent? foodComponent = null;

            foreach (var slot in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(slot) != itemComponent) continue;
                handsComponent.ActiveHand = slot;
                if (!entities.TryGetComponent(_target, out foodComponent))
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

            if ((!entities.EntityExists(_target) ? EntityLifeStage.Deleted : entities.GetComponent<MetaDataComponent>(_target).EntityLifeStage) >= EntityLifeStage.Deleted ||
                foodComponent.UsesRemaining == 0 ||
                entities.TryGetComponent(_owner, out HungerComponent? hungerComponent) &&
                hungerComponent.CurrentHunger >= hungerComponent.HungerThresholds[HungerThreshold.Okay])
            {
                return Outcome.Success;
            }

            return Outcome.Continuing;
        }
    }
}

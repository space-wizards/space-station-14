using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Hands.EntitySystems;
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
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var handsSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            // TODO: Also have this check storage a la backpack etc.
            if (entities.Deleted(_target) ||
                !entities.TryGetComponent(_owner, out HandsComponent? handsComponent))
            {
                return Outcome.Failed;
            }

            if (!handsSys.TrySelect<FoodComponent>(_owner, out var foodComponent, handsComponent))
                return Outcome.Failed;

            if (!handsSys.TryUseItemInHand(_owner, false, handsComponent))
                return Outcome.Failed;

            if ((!entities.EntityExists(_target) ? EntityLifeStage.Deleted : entities.GetComponent<MetaDataComponent>(_target).EntityLifeStage) >= EntityLifeStage.Deleted ||
                foodComponent.UsesRemaining == 0 ||
                entities.TryGetComponent(_owner, out HungerComponent? hungerComponent) &&
                hungerComponent.CurrentHunger >= hungerComponent.HungerThresholds[HungerThreshold.Okay])
            {
                return Outcome.Success;
            }

            /// do afters for food might mess this up?
            return Outcome.Continuing;
        }
    }
}

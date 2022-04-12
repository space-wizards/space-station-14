using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;

namespace Content.Server.AI.Operators.Nutrition
{
    public sealed class UseDrinkInInventoryOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _target;
        private float _interactionCooldown;

        public UseDrinkInInventoryOperator(EntityUid owner, EntityUid target)
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

            if (!handsSys.TrySelect<DrinkComponent>(_owner, out var drinkComponent, handsComponent))
                return Outcome.Failed;

            if (!handsSys.TryUseItemInHand(_owner, false, handsComponent))
                return Outcome.Failed;

            _interactionCooldown = IoCManager.Resolve<IRobustRandom>().NextFloat() + 0.5f;

            if (drinkComponent.Deleted || EntitySystem.Get<DrinkSystem>().IsEmpty(drinkComponent.Owner, drinkComponent)
                                       || entities.TryGetComponent(_owner, out ThirstComponent? thirstComponent) &&
                thirstComponent.CurrentThirst >= thirstComponent.ThirstThresholds[ThirstThreshold.Okay])
            {
                return Outcome.Success;
            }

            /// uuhhh do afters for drinks might mess this up?
            return Outcome.Continuing;
        }
    }
}

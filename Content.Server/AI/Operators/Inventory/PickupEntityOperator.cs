using Content.Server.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Server.AI.Operators.Inventory
{
    public sealed class PickupEntityOperator : AiOperator
    {
        // Input variables
        private readonly EntityUid _owner;
        private readonly EntityUid _target;

        public PickupEntityOperator(EntityUid owner, EntityUid target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var interactionSystem = sysMan.GetEntitySystem<InteractionSystem>();
            var handsSys = sysMan.GetEntitySystem<SharedHandsSystem>();

            if (entMan.Deleted(_target)
                || !entMan.HasComponent<ItemComponent>(_target)
                || _target.IsInContainer()
                || !interactionSystem.InRangeUnobstructed(_owner, _target, popup: true))
            {
                return Outcome.Failed;
            }

            // select empty hand
            if (!handsSys.TrySelectEmptyHand(_owner))
                return Outcome.Failed;

            interactionSystem.InteractHand(_owner, _target);
            return Outcome.Success;
        }
    }
}

using Content.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    public class PickupEntityOperator : IOperator
    {
        // Input variables
        private readonly IEntity _owner;
        private readonly IEntity _target;

        public PickupEntityOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        // TODO: When I spawn new entities they seem to duplicate clothing or something?
        public Outcome Execute(float frameTime)
        {
            if (_target == null ||
                _target.Deleted ||
                !_target.HasComponent<ItemComponent>() ||
                ContainerHelpers.IsInContainer(_target) ||
                (_owner.Transform.GridPosition.Position - _target.Transform.GridPosition.Position).Length >
                InteractionSystem.InteractionRange)
            {
                return Outcome.Failed;
            }

            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return Outcome.Failed;
            }

            var emptyHands = false;

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetHand(hand) == null)
                {
                    if (handsComponent.ActiveIndex != hand)
                    {
                        handsComponent.ActiveIndex = hand;
                    }

                    emptyHands = true;
                    break;
                }
            }

            if (!emptyHands)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.Interaction(_owner, _target);
            return Outcome.Success;
        }
    }
}

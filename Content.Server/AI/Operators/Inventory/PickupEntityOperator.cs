using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    public class PickupEntityOperator : IOperator
    {
        // Input variables
        private IEntity _owner;
        private IEntity _target;

        public PickupEntityOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        // TODO: When I spawn new entities they seem to duplicate clothing or something?
        public Outcome Execute(float frameTime)
        {
            // TODO: If they're in a locker need to check
            if (_target == null ||
                _target.Deleted ||
                !_target.TryGetComponent(out ItemComponent itemComponent) ||
                itemComponent.IsHeld ||
                (_owner.Transform.GridPosition.Position - _target.Transform.GridPosition.Position).Length >
                InteractionSystem.InteractionRange)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.Interaction(_owner, _target);
            return Outcome.Success;
        }
    }
}

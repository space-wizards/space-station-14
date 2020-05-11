using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// A Generic interacter; if you need to check stuff then make your own
    /// </summary>
    public class InteractWithEntityOperator : IOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _useTarget;

        public InteractWithEntityOperator(IEntity owner, IEntity useTarget)
        {
            _owner = owner;
            _useTarget = useTarget;

        }

        public Outcome Execute(float frameTime)
        {
            if (_useTarget.Transform.GridID != _owner.Transform.GridID)
            {
                return Outcome.Failed;
            }

            if ((_useTarget.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length > InteractionSystem.InteractionRange)
            {
                return Outcome.Failed;
            }

            if (_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            // Click on da thing
            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.UseItemInHand(_owner, _useTarget.Transform.GridPosition, _useTarget.Uid);

            return Outcome.Success;
        }
    }
}

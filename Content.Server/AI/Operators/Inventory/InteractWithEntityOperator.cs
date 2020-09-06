using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Utility;
using Content.Shared.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// A Generic interacter; if you need to check stuff then make your own
    /// </summary>
    public class InteractWithEntityOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _useTarget;

        public InteractWithEntityOperator(IEntity owner, IEntity useTarget)
        {
            _owner = owner;
            _useTarget = useTarget;

        }

        public override Outcome Execute(float frameTime)
        {
            if (_useTarget.Transform.GridID != _owner.Transform.GridID)
            {
                return Outcome.Failed;
            }

            if (!_owner.InRangeUnobstructed(_useTarget, popup: true))
            {
                return Outcome.Failed;
            }

            if (_owner.TryGetComponent(out CombatModeComponent combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            // Click on da thing
            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.UseItemInHand(_owner, _useTarget.Transform.Coordinates, _useTarget.Uid);

            return Outcome.Success;
        }
    }
}

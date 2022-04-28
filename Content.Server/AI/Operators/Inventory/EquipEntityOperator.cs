using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    public sealed class EquipEntityOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _entity;
        public EquipEntityOperator(EntityUid owner, EntityUid entity)
        {
            _owner = owner;
            _entity = entity;
        }

        public override Outcome Execute(float frameTime)
        {
            var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>();

            // TODO: If in clothing then click on it

            if (sys.TrySelect(_owner, _entity))
                return Outcome.Success;

            // TODO: Get free hand count; if no hands free then fail right here

            // TODO: Go through inventory
            return Outcome.Failed;
        }
    }
}

using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.AI.Operators.Inventory
{
    public sealed class DropEntityOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _entity;
        public DropEntityOperator(EntityUid owner, EntityUid entity)
        {
            _owner = owner;
            _entity = entity;
        }

        /// <summary>
        /// Requires EquipEntityOperator to put it in the active hand first
        /// </summary>
        /// <param name="frameTime"></param>
        /// <returns></returns>
        public override Outcome Execute(float frameTime)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>().TryDrop(_owner, _entity) ? Outcome.Success : Outcome.Failed;
        }
    }
}

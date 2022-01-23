using Content.Server.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    public class DropEntityOperator : AiOperator
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out HandsComponent? handsComponent))
            {
                return Outcome.Failed;
            }

            return handsComponent.Drop(_entity) ? Outcome.Success : Outcome.Failed;
        }
    }
}

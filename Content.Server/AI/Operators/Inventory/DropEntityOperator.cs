using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    public class DropEntityOperator : IOperator
    {
        private IEntity _owner;
        private IEntity _entity;
        public DropEntityOperator(IEntity owner, IEntity entity)
        {
            _owner = owner;
            _entity = entity;
        }

        /// <summary>
        /// Requires EquipEntityOperator to put it in the active hand first
        /// </summary>
        /// <param name="frameTime"></param>
        /// <returns></returns>
        public Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return Outcome.Failed;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetHand(hand).Owner == _entity)
                {
                    if (handsComponent.CanDrop(hand))
                    {
                        handsComponent.Drop(_entity);
                        return Outcome.Success;
                    }

                    return Outcome.Failed;
                }
            }

            return Outcome.Failed;
        }
    }
}

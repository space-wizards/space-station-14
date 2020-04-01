using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.HTN.Tasks.Primitive.Operators.Inventory
{
    public class DropHandItems : IOperator
    {
        private IEntity _owner;

        public DropHandItems(IEntity owner)
        {
            _owner = owner;
        }

        public Outcome Execute(float frameTime)
        {
            _owner.TryGetComponent(out HandsComponent handsComponent);
            foreach (var item in handsComponent.GetAllHeldItems())
            {
                handsComponent.Drop(item.Owner);
            }

            return Outcome.Success;
        }
    }
}

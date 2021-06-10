using Content.Server.Hands.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    public class DropHandItemsOperator : AiOperator
    {
        private readonly IEntity _owner;

        public DropHandItemsOperator(IEntity owner)
        {
            _owner = owner;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent? handsComponent))
            {
                return Outcome.Failed;
            }

            foreach (var item in handsComponent.GetAllHeldItems())
            {
                handsComponent.Drop(item.Owner);
            }

            return Outcome.Success;
        }
    }
}

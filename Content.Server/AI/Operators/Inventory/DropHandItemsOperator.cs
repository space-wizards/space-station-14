using Content.Server.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    public sealed class DropHandItemsOperator : AiOperator
    {
        private readonly EntityUid _owner;

        public DropHandItemsOperator(EntityUid owner)
        {
            _owner = owner;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out HandsComponent? handsComponent))
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

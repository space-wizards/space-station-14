using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;

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

            var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>();

            foreach (var hand in handsComponent.Hands.Values)
            {
                if (!hand.IsEmpty)
                    sys.TryDrop(_owner, hand, handsComp: handsComponent);
            }

            return Outcome.Success;
        }
    }
}

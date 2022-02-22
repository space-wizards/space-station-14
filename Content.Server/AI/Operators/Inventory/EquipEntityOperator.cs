using Content.Server.Hands.Components;
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
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(_owner, out HandsComponent? handsComponent))
            {
                return Outcome.Failed;
            }
            // TODO: If in clothing then click on it
            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(hand)?.Owner == _entity)
                {
                    handsComponent.ActiveHand = hand;
                    return Outcome.Success;
                }
            }

            // TODO: Get free hand count; if no hands free then fail right here

            // TODO: Go through inventory
            return Outcome.Failed;
        }
    }
}

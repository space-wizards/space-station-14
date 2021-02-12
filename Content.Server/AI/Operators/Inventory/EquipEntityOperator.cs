using Content.Server.GameObjects.Components.GUI;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    public sealed class EquipEntityOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _entity;
        public EquipEntityOperator(IEntity owner, IEntity entity)
        {
            _owner = owner;
            _entity = entity;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
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

using Content.Server.AI.HTN.Tasks.Primitive.Operators;
using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    public class SwapToFreeHandOperator : IOperator
    {
        private IEntity _owner;

        public SwapToFreeHandOperator(IEntity owner)
        {
            _owner = owner;
        }

        public Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return Outcome.Failed;
            }

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetHand(hand) == null)
                {
                    if (handsComponent.ActiveIndex != hand)
                    {
                        handsComponent.ActiveIndex = hand;
                    }
                    return Outcome.Success;
                }
            }

            return Outcome.Failed;
        }
    }
}

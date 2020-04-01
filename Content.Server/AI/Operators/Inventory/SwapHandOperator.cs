using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.HTN.Tasks.Primitive.Operators.Inventory
{
    public class SwapHandOperator : IOperator
    {
        private IEntity _owner;
        private string _hand;

        public SwapHandOperator(IEntity owner, string hand)
        {
            _owner = owner;
            _hand = hand;
        }

        public Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return Outcome.Failed;
            }

            handsComponent.ActiveIndex = _hand;
            return Outcome.Success;
        }
    }
}

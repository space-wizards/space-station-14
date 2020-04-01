using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.HTN.Tasks.Primitive.Operators.Inventory
{
    /// <summary>
    /// Will find the item in storage, put it in an active hand, then use it
    /// </summary>
    public class UseItemInHandsOperator : IOperator
    {
        // Output variables

        // Instance variables

        // Input variables
        private IEntity _owner;
        private IEntity _target;

        public UseItemInHandsOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        public Outcome Execute(float frameTime)
        {
            if (_target == null)
            {
                return Outcome.Failed;
            }

            // TODO: Also have this check storage a la backpack etc.
            _owner.TryGetComponent(out HandsComponent hands);
            _target.TryGetComponent(out ItemComponent itemComponent);

            foreach (var slot in hands.ActivePriorityEnumerable())
            {
                if (hands.GetHand(slot) != itemComponent) continue;
                hands.ActiveIndex = slot;
                hands.ActivateItem();
                return Outcome.Success;
            }

            return Outcome.Failed;
        }
    }
}

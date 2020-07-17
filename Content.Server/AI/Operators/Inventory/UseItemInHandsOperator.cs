using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.GUI;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Will find the item in storage, put it in an active hand, then use it
    /// </summary>
    public class UseItemInHandsOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _target;

        public UseItemInHandsOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_target == null)
            {
                return Outcome.Failed;
            }

            // TODO: Also have this check storage a la backpack etc.
            if (!_owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return Outcome.Failed;
            }

            if (!_target.TryGetComponent(out ItemComponent itemComponent))
            {
                return Outcome.Failed;
            }

            foreach (var slot in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(slot) != itemComponent) continue;
                handsComponent.ActiveIndex = slot;
                handsComponent.ActivateItem();
                return Outcome.Success;
            }

            return Outcome.Failed;
        }
    }
}

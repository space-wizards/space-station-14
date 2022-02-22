using Content.Server.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// Will find the item in storage, put it in an active hand, then use it
    /// </summary>
    public sealed class UseItemInInventoryOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _target;

        public UseItemInInventoryOperator(EntityUid owner, EntityUid target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            // TODO: Also have this check storage a la backpack etc.
            if (!entMan.TryGetComponent(_owner, out HandsComponent? handsComponent))
            {
                return Outcome.Failed;
            }

            if (!entMan.TryGetComponent(_target, out SharedItemComponent? itemComponent))
            {
                return Outcome.Failed;
            }

            foreach (var slot in handsComponent.ActivePriorityEnumerable())
            {
                if (handsComponent.GetItem(slot) != itemComponent) continue;
                handsComponent.ActiveHand = slot;
                handsComponent.ActivateItem();
                return Outcome.Success;
            }

            return Outcome.Failed;
        }
    }
}

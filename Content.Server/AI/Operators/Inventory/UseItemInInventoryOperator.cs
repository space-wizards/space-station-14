using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;
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
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var sys = sysMan.GetEntitySystem<SharedHandsSystem>();

            // TODO: Also have this check storage a la backpack etc.
            if (!entMan.TryGetComponent(_owner, out HandsComponent? handsComponent)
                || !sys.TrySelect(_owner, _target, handsComponent)
                || !sys.TryUseItemInHand(_owner, false, handsComponent))
            {
                return Outcome.Failed;
            }

            return Outcome.Success;
        }
    }
}

using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Inventory
{
    [UsedImplicitly]
    public sealed class EnumerableInventoryState : StateData<IEnumerable<EntityUid>>
    {
        public override string Name => "EnumerableInventory";

        public override IEnumerable<EntityUid> GetValue()
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedHandsSystem>().EnumerateHeld(Owner);
        }
    }
}

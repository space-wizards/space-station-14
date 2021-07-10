using Content.Server.Inventory.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Inventory
{
    class InventorySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanInventoryControllerComponent, EntRemovedFromContainerMessage>(HandleRemovedFromContainer);
            SubscribeLocalEvent<InventoryComponent, EntRemovedFromContainerMessage>(HandleInvRemovedFromContainer);
        }

        private static void HandleInvRemovedFromContainer(EntityUid uid, InventoryComponent component, EntRemovedFromContainerMessage args)
        {
            component.ForceUnequip(args.Container, args.Entity);
        }

        private static void HandleRemovedFromContainer(EntityUid uid, HumanInventoryControllerComponent component, EntRemovedFromContainerMessage args)
        {
            component.CheckUniformExists();
        }
    }
}

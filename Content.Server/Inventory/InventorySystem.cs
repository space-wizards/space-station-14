using Content.Server.Inventory.Components;
using Robust.Server.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Inventory
{
    class InventorySystem : EntitySystem
    {
        [Dependency] private readonly IConGroupController _groupController = default!;

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

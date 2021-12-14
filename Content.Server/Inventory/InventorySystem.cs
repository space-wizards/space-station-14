using Content.Server.Atmos;
using Content.Server.Inventory.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Inventory
{
    class ServerInventorySystem : InventorySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HumanInventoryControllerComponent, EntRemovedFromContainerMessage>(HandleRemovedFromContainer);

            SubscribeLocalEvent<InventoryComponent, EntRemovedFromContainerMessage>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(RelayInventoryEvent);
        }

        private void HandleRemovedFromContainer(EntityUid uid, HumanInventoryControllerComponent component, EntRemovedFromContainerMessage args)
        {
            component.CheckUniformExists();
        }

        private void HandleInvRemovedFromContainer(EntityUid uid, InventoryComponent component, EntRemovedFromContainerMessage args)
        {
            component.ForceUnequip(args.Container, args.Entity);
        }
    }
}

using Content.Server.Atmos;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Inventory
{
    class ServerInventorySystem : InventorySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(RelayInventoryEvent);

            SubscribeNetworkEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);
        }

        private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev)
        {
            if (TryGetSlotEntity(ev.Uid, ev.Slot, out var entityUid) && TryComp<ServerStorageComponent>(entityUid, out var storageComponent))
            {
                storageComponent.OpenStorageUI(ev.Uid);
            }
        }
    }
}

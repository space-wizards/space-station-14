using Content.Server.Atmos;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Inventory
{
    sealed class ServerInventorySystem : InventorySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(RelayInventoryEvent);

            SubscribeNetworkEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);
        }

        private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not EntityUid { Valid: true } uid)
                    return;

            if (TryGetSlotEntity(uid, ev.Slot, out var entityUid) && TryComp<ServerStorageComponent>(entityUid, out var storageComponent))
            {
                storageComponent.OpenStorageUI(uid);
            }
        }
    }
}

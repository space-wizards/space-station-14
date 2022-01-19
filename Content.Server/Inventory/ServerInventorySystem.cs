using Content.Server.Atmos;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Inventory
{
    class ServerInventorySystem : InventorySystem
    {
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, HighPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, LowPressureEvent>(RelayInventoryEvent);
            SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(RelayInventoryEvent);

            SubscribeNetworkEvent<TryEquipNetworkMessage>(OnNetworkEquip);
            SubscribeNetworkEvent<TryUnequipNetworkMessage>(OnNetworkUnequip);
            SubscribeNetworkEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);
            SubscribeNetworkEvent<UseSlotNetworkMessage>(OnUseSlot);
        }

        private void OnUseSlot(UseSlotNetworkMessage ev)
        {
            if (!TryComp<HandsComponent>(ev.Uid, out var hands) || !TryGetSlotEntity(ev.Uid, ev.Slot, out var itemUid))
                return;

            var activeHand = hands.GetActiveHand;
            if (activeHand != null)
            {
                _interactionSystem.InteractUsing(ev.Uid, activeHand.Owner, itemUid.Value,
                    new EntityCoordinates());
            }
            else if (TryUnequip(ev.Uid, ev.Slot))
            {
                hands.PutInHand(itemUid.Value);
            }
        }

        private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev)
        {
            if (TryGetSlotEntity(ev.Uid, ev.Slot, out var entityUid) && TryComp<ServerStorageComponent>(entityUid, out var storageComponent))
            {
                storageComponent.OpenStorageUI(ev.Uid);
            }
        }

        private void OnNetworkUnequip(TryUnequipNetworkMessage ev)
        {
            TryUnequip(ev.Actor, ev.Target, ev.Slot, ev.Silent, ev.Force);
        }

        private void OnNetworkEquip(TryEquipNetworkMessage ev)
        {
            TryEquip(ev.Actor, ev.Target, ev.ItemUid, ev.Slot, ev.Silent, ev.Force);
        }
    }
}

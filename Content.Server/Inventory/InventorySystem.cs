using Content.Server.Atmos;
using Content.Server.Inventory.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
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

            SubscribeNetworkEvent<TryEquipNetworkMessage>(OnNetworkEquip);
            SubscribeNetworkEvent<TryUnequipNetworkMessage>(OnNetworkUnequip);
        }

        private void OnNetworkUnequip(TryUnequipNetworkMessage ev)
        {
            TryUnequip(ev.Uid, ev.Slot, ev.Silent, ev.Force);
        }

        private void OnNetworkEquip(TryEquipNetworkMessage ev)
        {
            TryEquip(ev.Uid, ev.ItemUid, ev.Slot, ev.Silent, ev.Force);
        }

        private void HandleRemovedFromContainer(EntityUid uid, HumanInventoryControllerComponent component, EntRemovedFromContainerMessage args)
        {
            component.CheckUniformExists();
        }
    }
}

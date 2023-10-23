using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;

namespace Content.Server.Inventory
{
    public sealed class ServerInventorySystem : InventorySystem
    {
        [Dependency] private readonly StorageSystem _storageSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);

            SubscribeNetworkEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);
        }

        private void OnUseInHand(EntityUid uid, ClothingComponent component, UseInHandEvent args)
        {
            if (args.Handled || !component.QuickEquip)
                return;

            QuickEquip(uid, component, args);
        }

        private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
                    return;

            if (TryGetSlotEntity(uid, ev.Slot, out var entityUid) && TryComp<StorageComponent>(entityUid, out var storageComponent))
            {
                _storageSystem.OpenStorageUI(entityUid.Value, uid, storageComponent);
            }
        }

        public void TransferEntityInventories(EntityUid uid, EntityUid target)
        {
            if (!TryGetContainerSlotEnumerator(uid, out var enumerator))
                return;

            Dictionary<string, EntityUid> inventoryEntities = new();
            var slots = GetSlots(uid);
            while (enumerator.MoveNext(out var containerSlot))
            {
                //records all the entities stored in each of the target's slots
                foreach (var slot in slots)
                {
                    if (TryGetSlotContainer(target, slot.Name, out var conslot, out _) &&
                        conslot.ID == containerSlot.ID &&
                        containerSlot.ContainedEntity is { } containedEntity)
                    {
                        inventoryEntities.Add(slot.Name, containedEntity);
                    }
                }
                //drops everything in the target's inventory on the ground
                TryUnequip(uid, containerSlot.ID, true, true);
            }
            // This takes the objects we removed and stored earlier
            // and actually equips all of it to the new entity
            foreach (var (slot, item) in inventoryEntities)
            {
                TryEquip(target, item, slot , true, true);
            }
        }
    }
}

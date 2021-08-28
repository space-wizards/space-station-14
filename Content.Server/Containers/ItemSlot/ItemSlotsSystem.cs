using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Containers.ItemSlots
{
    public class ItemSlotsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemSlotsComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ItemSlotsComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<ItemSlotsComponent, PlaceItemAttempt>(OnPlaceItem);
            SubscribeLocalEvent<ItemSlotsComponent, EjectItemAttempt>(OnEjectItem);
        }

        private void OnComponentInit(EntityUid uid, ItemSlotsComponent itemSlots, ComponentInit args)
        {
            foreach (var pair in itemSlots.Slots)
            {
                var slotName = pair.Key;
                var slot = pair.Value;

                slot.ContainerSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(itemSlots.Owner, slotName);
            }
        }

        private void OnMapInit(EntityUid uid, ItemSlotsComponent itemSlots, MapInitEvent args)
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!string.IsNullOrEmpty(slot.StartingItem))
                {
                    var entManager = itemSlots.Owner.EntityManager;

                    var item = entManager.SpawnEntity(slot.StartingItem, itemSlots.Owner.Transform.Coordinates);
                    slot.ContainerSlot.Insert(item);
                }
            }
        }

        private void OnInteractUsing(EntityUid uid, ItemSlotsComponent itemSlots, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = TryInsertContent(itemSlots, args.Used, args.User);
        }

        private void OnPlaceItem(EntityUid uid, ItemSlotsComponent slots, PlaceItemAttempt args)
        {
            if (!slots.Slots.TryGetValue(args.SlotName, out var slot))
            {
                args.Cancel();
                return;
            }

            if (slot.ContainerSlot.ContainedEntity != null)
            {
                args.Cancel();
                return;
            }

            slot.ContainerSlot.Insert(args.Item);
            RaiseLocalEvent(uid, new ItemSlotChanged(slots, args.SlotName, slot));
        }

        private void OnEjectItem(EntityUid uid, ItemSlotsComponent component, EjectItemAttempt args)
        {
            TryEjectContent(component, args.SlotName, args.User);
        }

        /// <summary>
        ///     Tries to insert item in any fiting item slot
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertContent(ItemSlotsComponent itemSlots, IEntity item, IEntity user)
        {
            foreach (var pair in itemSlots.Slots)
            {
                var slotName = pair.Key;
                var slot = pair.Value;

                // check if item allowed in whitelist
                if (slot.Whitelist != null && !slot.Whitelist.IsValid(item))
                    continue;

                // check if slot is empty
                if (slot.ContainerSlot.Contains(item))
                    continue;

                if (!user.TryGetComponent(out IHandsComponent? hands))
                {
                    itemSlots.Owner.PopupMessage(user, Loc.GetString("item-slots-try-insert-no-hands"));
                    return true;
                }

                // get item inside container
                IEntity? swap = null;
                if (slot.ContainerSlot.ContainedEntity != null)
                    swap = slot.ContainerSlot.ContainedEntity;

                // return if user can't drop active item in hand
                if (!hands.Drop(item))
                    return true;

                // swap item in hand and item in slot
                if (swap != null)
                    hands.PutInHand(swap.GetComponent<ItemComponent>());

                // insert item
                slot.ContainerSlot.Insert(item);
                RaiseLocalEvent(itemSlots.Owner.Uid, new ItemSlotChanged(itemSlots, slotName, slot));

                // play sound
                if (slot.InsertSound != null)
                    SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.InsertSound.GetSound(), itemSlots.Owner);

                return true;
            }

            return false;
        }


        public IEntity? PeekItemInSlot(ItemSlotsComponent itemSlots, string slotName)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return null;

            var item = slot.ContainerSlot.ContainedEntity;
            return item;
        }

        /// <summary>
        ///     Try to eject item from slot to users hands
        /// </summary>
        public void TryEjectContent(ItemSlotsComponent itemSlots, string slotName, IEntity? user)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return;

            if (slot.ContainerSlot.ContainedEntity == null)
                return;

            var item = slot.ContainerSlot.ContainedEntities[0];
            slot.ContainerSlot.Remove(item);

            if (user != null)
            {
                var hands = user.GetComponent<HandsComponent>();
                var itemComponent = item.GetComponent<ItemComponent>();
                hands.PutInHandOrDrop(itemComponent);
            }

            if (slot.EjectSound != null)
                SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.EjectSound.GetSound(), itemSlots.Owner);

            RaiseLocalEvent(itemSlots.Owner.Uid, new ItemSlotChanged(itemSlots, slotName, slot));
        }
    }
}

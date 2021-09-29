using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Shared.Containers.ItemSlots
{
    public class SharedItemSlotsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedItemSlotsComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SharedItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnComponentInit(EntityUid uid, SharedItemSlotsComponent itemSlots, ComponentInit args)
        {
            // create container for each slot 
            foreach (var pair in itemSlots.Slots)
            {
                var slotName = pair.Key;
                var slot = pair.Value;

                slot.ContainerSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(itemSlots.Owner, slotName);
            }
        }

        private void OnMapInit(EntityUid uid, SharedItemSlotsComponent itemSlots, MapInitEvent args)
        {
            foreach (var pair in itemSlots.Slots)
            {
                var slot = pair.Value;
                var slotName = pair.Key;

                // Check if someone already put item inside container
                if (slot.ContainerSlot.ContainedEntity != null)
                    continue;

                // Try to spawn item inside each slot
                if (!string.IsNullOrEmpty(slot.StartingItem))
                {
                    var item = EntityManager.SpawnEntity(slot.StartingItem, itemSlots.Owner.Transform.Coordinates);
                    slot.ContainerSlot.Insert(item);

                    RaiseLocalEvent(uid, new ItemSlotChanged(itemSlots, slotName, slot));
                }
            }
        }

        private void OnInteractUsing(EntityUid uid, SharedItemSlotsComponent itemSlots, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = TryInsertContent(itemSlots, args.Used, args.User);
        }

        /// <summary>
        ///     Tries to insert item in any fitting item slot from users hand
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertContent(SharedItemSlotsComponent itemSlots, IEntity item, IEntity user)
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

                if (!user.TryGetComponent(out SharedHandsComponent? hands))
                {
                    itemSlots.Owner.PopupMessage(user, Loc.GetString("item-slots-try-insert-no-hands"));
                    return true;
                }

                // get item inside container
                IEntity? swap = null;
                if (slot.ContainerSlot.ContainedEntity != null)
                    swap = slot.ContainerSlot.ContainedEntity;

                // return if user can't drop active item in hand
                if (!hands.TryDropEntityToFloor(item))
                    return true;

                // swap item in hand and item in slot
                if (swap != null)
                    hands.TryPutInAnyHand(swap);

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

        /// <summary>
        ///     Tries to insert item in known slot. Doesn't interact with user
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertContent(SharedItemSlotsComponent itemSlots, IEntity item, string slotName)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return false;

            if (slot.ContainerSlot.ContainedEntity != null)
                return false;

            // check if item allowed in whitelist
            if (slot.Whitelist != null && !slot.Whitelist.IsValid(item))
                return false;

            slot.ContainerSlot.Insert(item);
            RaiseLocalEvent(itemSlots.Owner.Uid, new ItemSlotChanged(itemSlots, slotName, slot));
            return true;
        }

        /// <summary>
        ///     Check if slot has some content in it (without ejecting item)
        /// </summary>
        /// <returns>Null if doesn't have any content</returns>
        public IEntity? PeekItemInSlot(SharedItemSlotsComponent itemSlots, string slotName)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return null;

            var item = slot.ContainerSlot.ContainedEntity;
            return item;
        }

        /// <summary>
        ///     Try to eject item from slot to users hands
        /// </summary>
        public bool TryEjectContent(SharedItemSlotsComponent itemSlots, string slotName, IEntity? user)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return false;

            if (slot.ContainerSlot.ContainedEntity == null)
                return false;

            var item = slot.ContainerSlot.ContainedEntity;
            if (!slot.ContainerSlot.Remove(item))
                return false;

            // try eject item to users hand
            if (user != null)
            {
                if (user.TryGetComponent(out SharedHandsComponent? hands))
                {
                    hands.TryPutInAnyHand(item);
                }
                else
                {
                    itemSlots.Owner.PopupMessage(user, Loc.GetString("item-slots-try-insert-no-hands"));
                }
            }

            if (slot.EjectSound != null)
                SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.EjectSound.GetSound(), itemSlots.Owner);

            RaiseLocalEvent(itemSlots.Owner.Uid, new ItemSlotChanged(itemSlots, slotName, slot));
            return true;
        }
    }
}

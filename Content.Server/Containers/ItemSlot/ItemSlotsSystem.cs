using Content.Server.Hands.Components;
using Content.Server.Items;
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

            args.Handled = TryInsertContent(itemSlots, args);
        }

        private bool TryInsertContent(ItemSlotsComponent itemSlots, InteractUsingEvent eventArgs)
        {
            var item = eventArgs.Used;
            foreach (var pair in itemSlots.Slots)
            {
                var slotName = pair.Key;
                var slot = pair.Value;

                if (slot.Whitelist != null && !slot.Whitelist.IsValid(item))
                    continue;

                if (slot.ContainerSlot.Contains(item))
                    continue;

                if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
                {
                    itemSlots.Owner.PopupMessage(eventArgs.User, Loc.GetString("item-slot-try-insert-no-hands"));
                    return true;
                }

                IEntity? swap = null;
                if (slot.ContainerSlot.ContainedEntity != null)
                    swap = slot.ContainerSlot.ContainedEntities[0];

                if (!hands.Drop(item))
                    return true;

                if (swap != null)
                    hands.PutInHand(swap.GetComponent<ItemComponent>());

                // Insert item
                slot.ContainerSlot.Insert(item);
                RaiseLocalEvent(new ItemSlotChanged(itemSlots, slotName, slot));

                // Play sound
                if (slot.InsertSound != null)
                    SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.InsertSound.GetSound(), itemSlots.Owner);

                return true;
            }

            return false;
        }

        public void TryEjectContent(ItemSlotsComponent itemSlots, string slotName, IEntity user)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out ItemSlot? slot))
                return;

            if (slot.ContainerSlot.ContainedEntity == null)
                return;

            var item = slot.ContainerSlot.ContainedEntities[0];
            slot.ContainerSlot.Remove(item);

            var hands = user.GetComponent<HandsComponent>();
            var itemComponent = item.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(itemComponent);

            RaiseLocalEvent(new ItemSlotChanged(itemSlots, slotName, slot));

            if (slot.EjectSound != null)
                SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.EjectSound.GetSound(), itemSlots.Owner);
        }
    }
}

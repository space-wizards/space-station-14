using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Shared.Containers.ItemSlots
{
    public class SharedItemSlotsSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedItemSlotsComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SharedItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<SharedItemSlotsComponent, GetAlternativeVerbsEvent>(AddEjectVerbs);
            SubscribeLocalEvent<SharedItemSlotsComponent, GetInteractionVerbsEvent>(AddInsertVerbs);
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

                    RaiseLocalEvent(uid, new ItemSlotChangedEvent(itemSlots, slotName, slot));
                }
            }
        }

        private void AddEjectVerbs(EntityUid uid, SharedItemSlotsComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanPickup(args.User.Uid))
                return;

            foreach (var (slotName, slot) in component.Slots)
            {
                if (slot.ContainerSlot.ContainedEntity == null)
                    continue;

                Verb verb = new();
                verb.Text = slot.Name;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => TryEjectContent(uid, slotName, args.User, component);

                args.Verbs.Add(verb);
            }
        }

        private void AddInsertVerbs(EntityUid uid, SharedItemSlotsComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            foreach (var (slotName, slot) in component.Slots)
            {
                if (!CanInsertContent(args.Using, slot))
                    continue;

                Verb verb = new();
                verb.Text = slot.Name != string.Empty ? slot.Name : args.Using.Name;
                verb.Category = VerbCategory.Insert;
                verb.Act = () => InsertContent(component, slot, slotName, args.Using);
                args.Verbs.Add(verb);
            }
        }

        private void OnInteractUsing(EntityUid uid, SharedItemSlotsComponent itemSlots, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = TryInsertContent(uid, args.Used, args.User, itemSlots);
        }

        /// <summary>
        ///     Tries to insert or swap an item in any fitting item slot from users hand. If a valid slot already contains an item, it will swap it out.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertContent(EntityUid uid, IEntity item, IEntity user, SharedItemSlotsComponent? itemSlots = null, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return false;

            if (!Resolve(user.Uid, ref hands))
            {
                itemSlots.Owner.PopupMessage(user, Loc.GetString("item-slots-try-insert-no-hands"));
                return false;
            }

            foreach (var (slotName, slot) in itemSlots.Slots)
            {
                // check if item allowed in whitelist
                if (slot.Whitelist != null && !slot.Whitelist.IsValid(item.Uid))
                    continue;

                // check if slot does not contain the item currently being inserted???
                if (slot.ContainerSlot.Contains(item))
                    continue;

                // get item inside container
                IEntity? swap = null;
                if (slot.ContainerSlot.ContainedEntity != null)
                    swap = slot.ContainerSlot.ContainedEntity;

                // return if user can't drop active item in hand
                if (!hands.Drop(item))
                    return true;

                // swap item in hand and item in slot
                if (swap != null)
                    hands.TryPutInAnyHand(swap);

                InsertContent(itemSlots, slot, slotName, item);

                return true;
            }

            return false;
        }

        public void InsertContent(SharedItemSlotsComponent itemSlots, ItemSlot slot, string slotName, IEntity item)
        {
            // insert item
            slot.ContainerSlot.Insert(item);
            RaiseLocalEvent(itemSlots.OwnerUid, new ItemSlotChangedEvent(itemSlots, slotName, slot));

            // play sound
            if (slot.InsertSound != null)
                SoundSystem.Play(Filter.Pvs(itemSlots.Owner), slot.InsertSound.GetSound(), itemSlots.Owner);
        }

        /// <summary>
        ///     Can a given item be inserted into a slot, without ejecting the current item in that slot.
        /// </summary>
        public bool CanInsertContent(IEntity item, ItemSlot slot)
        {
            if (slot.ContainerSlot.ContainedEntity != null)
                return false;

            // check if item allowed in whitelist
            if (slot.Whitelist != null && !slot.Whitelist.IsValid(item.Uid))
                return false;

            return true;
        }

        /// <summary>
        ///     Tries to insert item in known slot. Doesn't interact with user
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertContent(SharedItemSlotsComponent itemSlots, IEntity item, string slotName)
        {
            if (!itemSlots.Slots.TryGetValue(slotName, out var slot))
                return false;

            if (!CanInsertContent(item, slot))
                return false;

            InsertContent(itemSlots, slot, slotName, item);
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
        public bool TryEjectContent(EntityUid uid, string slotName, IEntity? user, SharedItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return false;

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

            RaiseLocalEvent(itemSlots.OwnerUid, new ItemSlotChangedEvent(itemSlots, slotName, slot));
            return true;
        }
    }
}

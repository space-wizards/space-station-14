using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Containers.ItemSlots
{
    /// <summary>
    ///     A class that handles interactions related to inserting/ejecting items into/from an item slot.
    /// </summary>
    public class ItemSlotsSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemSlotsComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ItemSlotsComponent, ComponentInit>(Oninitialize);

            SubscribeLocalEvent<ItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ItemSlotsComponent, InteractHandEvent>(OnInteractHand);

            SubscribeLocalEvent<ItemSlotsComponent, GetAlternativeVerbsEvent>(AddEjectVerbs);
            SubscribeLocalEvent<ItemSlotsComponent, GetInteractionVerbsEvent>(AddInteractionVerbsVerbs);

            SubscribeLocalEvent<ItemSlotsComponent, ComponentGetState>(GetItemSlotsState);
            SubscribeLocalEvent<ItemSlotsComponent, ComponentHandleState>(HandleItemSlotsState);
        }

        #region ComponentManagement
        /// <summary>
        ///     Spawn in starting items for any item slots that should have one.
        /// </summary>
        private void OnStartup(EntityUid uid, ItemSlotsComponent itemSlots, ComponentStartup args)
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.HasItem || string.IsNullOrEmpty(slot.StartingItem))
                    continue;

                var item = EntityManager.SpawnEntity(slot.StartingItem, itemSlots.Owner.Transform.Coordinates);
                slot.ContainerSlot.Insert(item);
            }
        }

        /// <summary>
        ///     Ensure item slots have containers.
        /// </summary>
        private void Oninitialize(EntityUid uid, ItemSlotsComponent itemSlots, ComponentInit args)
        {
            foreach (var (id, slot) in itemSlots.Slots)
            {
                slot.ContainerSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(itemSlots.Owner, id);
            }
        }

        /// <summary>
        ///     Given a new item slot, store it in the <see cref="ItemSlotsComponent"/> and ensure the slot has an item
        ///     container.
        /// </summary>
        public void AddItemSlot(EntityUid uid, string id, ItemSlot slot)
        {
            var itemSlots = EntityManager.EnsureComponent<ItemSlotsComponent>(uid);
            slot.ContainerSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(itemSlots.Owner, id);
            DebugTools.Assert(!itemSlots.Slots.ContainsKey(id));
            itemSlots.Slots[id] = slot;
        }

        /// <summary>
        ///     Remove an item slot. This should generally be called whenever a component that added a slot is being
        ///     removed.
        /// </summary>
        public void RemoveItemSlot(EntityUid uid, ItemSlot slot, ItemSlotsComponent? itemSlots = null)
        {
            slot.ContainerSlot.Shutdown();

            // Don't log missing resolves. when an entity has all of its components removed, the ItemSlotsComponent may
            // have been removed before some other component that added an item slot (and is now trying to remove it).
            if (!Resolve(uid, ref itemSlots, logMissing: false))
                return;

            itemSlots.Slots.Remove(slot.ID);

            if (itemSlots.Slots.Count == 0)
                EntityManager.RemoveComponent(uid, itemSlots);
        }
        #endregion

        #region Interactions
        /// <summary>
        ///     Attempt to take an item from a slot, if any are set to EjectOnInteract.
        /// </summary>
        private void OnInteractHand(EntityUid uid, ItemSlotsComponent itemSlots, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.Locked || !slot.EjectOnInteract || slot.Item == null)
                    continue;

                args.Handled = true;
                TryEjectToHands(uid, slot, args.UserUid);
                break;
            }
        }

        /// <summary>
        ///     Tries to insert a held item in any fitting item slot. If a valid slot already contains an item, it will
        ///     swap it out and place the old one in the user's hand.
        /// </summary>
        /// <remarks>
        ///     This only handles the event if the user has an applicable entity that can be inserted. This allows for
        ///     other interactions to still happen (e.g., open UI, or toggle-open), despite the user holding an item.
        ///     Maybe this is undesirable.
        /// </remarks>
        private void OnInteractUsing(EntityUid uid, ItemSlotsComponent itemSlots, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(args.UserUid, out SharedHandsComponent? hands))
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!CanInsert(args.UsedUid, slot, swap: true))
                    continue;

                // Drop the held item onto the floor. Return if the user cannot drop.
                if (!hands.Drop(args.Used))
                    return;

                if (slot.Item != null)
                    hands.TryPutInAnyHand(slot.Item);

                Insert(uid, slot, args.Used);
                args.Handled = true;
                return;
            }
        }
        #endregion

        #region Insert
        /// <summary>
        ///     Insert an item into a slot. This does not perform checks, so make sure to also use <see
        ///     cref="CanInsert"/> or just use <see cref="TryInsert"/> instead.
        /// </summary>
        private void Insert(EntityUid uid, ItemSlot slot, IEntity item)
        {
            slot.ContainerSlot.Insert(item);
            // ContainerSlot automatically raises a directed EntInsertedIntoContainerMessage

            if (slot.InsertSound != null)
                SoundSystem.Play(Filter.Pvs(uid), slot.InsertSound.GetSound(), uid);
        }

        /// <summary>
        ///     Check whether a given item can be inserted into a slot. Unless otherwise specified, this will return
        ///     false if the slot is already filled.
        /// </summary>
        public bool CanInsert(EntityUid uid, ItemSlot slot, bool swap = false)
        {
            if (slot.Locked)
                return false;

            if (!swap && slot.HasItem)
                return false;

            if (slot.Whitelist != null && !slot.Whitelist.IsValid(uid))
                return false;

            // We should also check ContainerSlot.CanInsert, but that prevents swapping interactions. Given that
            // ContainerSlot.CanInsert gets called when the item is actually inserted anyways, we can just get away with
            // fudging CanInsert and not performing those checks.
            return true;
        }

        /// <summary>
        ///     Tries to insert item into a specific slot.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsert(EntityUid uid, string id, IEntity item, ItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return false;

            if (!itemSlots.Slots.TryGetValue(id, out var slot))
                return false;

            return TryInsert(uid, slot, item);
        }

        /// <summary>
        ///     Tries to insert item into a specific slot.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsert(EntityUid uid, ItemSlot slot, IEntity item)
        {
            if (!CanInsert(item.Uid, slot))
                return false;

            Insert(uid, slot, item);
            return true;
        }
        #endregion

        #region Eject
        /// <summary>
        ///     Eject an item into a slot. This does not perform checks (e.g., is the slot locked?), so you should
        ///     probably just use <see cref="TryEject"/> instead.
        /// </summary>
        private void Eject(EntityUid uid, ItemSlot slot, IEntity item)
        {
            slot.ContainerSlot.Remove(item);
            // ContainerSlot automatically raises a directed EntRemovedFromContainerMessage

            if (slot.EjectSound != null)
                SoundSystem.Play(Filter.Pvs(uid), slot.EjectSound.GetSound(), uid);
        }

        /// <summary>
        ///     Try to eject an item from a slot.
        /// </summary>
        /// <returns>False if item slot is locked or has no item inserted</returns>
        public bool TryEject(EntityUid uid, ItemSlot slot, [NotNullWhen(true)] out IEntity? item)
        {
            item = null;

            if (slot.Locked || slot.Item == null)
                return false;

            item = slot.Item;
            Eject(uid, slot, item);
            return true;
        }

        /// <summary>
        ///     Try to eject item from a slot.
        /// </summary>
        /// <returns>False if the id is not valid, the item slot is locked, or it has no item inserted</returns>
        public bool TryEject(EntityUid uid, string id, [NotNullWhen(true)] out IEntity? item, ItemSlotsComponent? itemSlots = null)
        {
            item = null;

            if (!Resolve(uid, ref itemSlots))
                return false;

            if (!itemSlots.Slots.TryGetValue(id, out var slot))
                return false;

            return TryEject(uid, slot, out item);
        }

        /// <summary>
        ///     Try to eject item from a slot directly into a user's hands. If they have no hands, the item will still
        ///     be ejected onto the floor.
        /// </summary>
        /// <returns>
        ///     False if the id is not valid, the item slot is locked, or it has no item inserted. True otherwise, even
        ///     if the user has no hands.
        /// </returns>
        public bool TryEjectToHands(EntityUid uid, ItemSlot slot, EntityUid? user)
        {
            if (!TryEject(uid, slot, out var item))
                return false;

            if (user != null && EntityManager.TryGetComponent(user.Value, out SharedHandsComponent? hands))
                hands.TryPutInAnyHand(item);

            return true;
        }
        #endregion

        #region Verbs
        private void AddEjectVerbs(EntityUid uid, ItemSlotsComponent itemSlots, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess ||!args.CanInteract ||
                !_actionBlockerSystem.CanPickup(args.User.Uid))
            {
                return;
            }

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.Locked || !slot.HasItem)
                    continue;

                if (slot.EjectOnInteract)
                    // For this item slot, ejecting/inserting is a primary interaction. Instead of an eject category
                    // alt-click verb, there will be a "Take item" primary interaction verb.
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : slot.Item!.Name ?? string.Empty;

                Verb verb = new();
                verb.Act = () => TryEjectToHands(uid, slot, args.User.Uid);

                if (slot.EjectVerbText == null)
                {
                    verb.Text = verbSubject;
                    verb.Category = VerbCategory.Eject;
                }
                else
                {
                    verb.Text = Loc.GetString(slot.EjectVerbText);
                    verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                }

                args.Verbs.Add(verb);
            }
        }

        private void AddInteractionVerbsVerbs(EntityUid uid, ItemSlotsComponent itemSlots, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // If there are any slots that eject on left-click, add a "Take <item>" verb.
            if (_actionBlockerSystem.CanPickup(args.User.Uid))
            {
                foreach (var slot in itemSlots.Slots.Values)
                {
                    if (!slot.EjectOnInteract || slot.Locked || !slot.HasItem)
                        continue;

                    var verbSubject = slot.Name != string.Empty
                        ? Loc.GetString(slot.Name)
                        : slot.Item!.Name ?? string.Empty;

                    Verb takeVerb = new();
                    takeVerb.Act = () => TryEjectToHands(uid, slot, args.User.Uid);
                    takeVerb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

                    if (slot.EjectVerbText == null)
                        takeVerb.Text = Loc.GetString("take-item-verb-text", ("subject", verbSubject));
                    else
                        takeVerb.Text = Loc.GetString(slot.EjectVerbText);

                    args.Verbs.Add(takeVerb);
                }
            }

            // Next, add the insert-item verbs
            if (args.Using == null || !_actionBlockerSystem.CanDrop(args.User.Uid))
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!CanInsert(args.Using.Uid, slot))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : args.Using.Name ?? string.Empty;

                Verb insertVerb = new();
                insertVerb.Act = () => Insert(uid, slot, args.Using);

                if (slot.InsertVerbText != null)
                {
                    insertVerb.Text = Loc.GetString(slot.InsertVerbText);
                    insertVerb.IconTexture = "/Textures/Interface/VerbIcons/insert.svg.192dpi.png";
                }
                else if(slot.EjectOnInteract)
                {
                    // Inserting/ejecting is a primary interaction for this entity. Instead of using the insert
                    // category, we will use a single "Place <item>" verb.
                    insertVerb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
                    insertVerb.IconTexture = "/Textures/Interface/VerbIcons/drop.svg.192dpi.png";
                }
                else
                {
                    insertVerb.Category = VerbCategory.Insert;
                    insertVerb.Text = verbSubject;
                }

                args.Verbs.Add(insertVerb);
            }
        }
        #endregion

        /// <summary>
        ///     Get the contents of some item slot.
        /// </summary>
        public IEntity? GetItem(EntityUid uid, string id, ItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return null;

            return itemSlots.Slots.GetValueOrDefault(id)?.Item;
        }

        /// <summary>
        ///     Lock an item slot. This stops items from being inserted into or ejected from this slot.
        /// </summary>
        public void SetLock(EntityUid uid, string id, bool locked, ItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return;

            if (!itemSlots.Slots.TryGetValue(id, out var slot))
                return;

            SetLock(itemSlots, slot, locked);
        }

        /// <summary>
        ///     Lock an item slot. This stops items from being inserted into or ejected from this slot.
        /// </summary>
        public void SetLock(ItemSlotsComponent itemSlots, ItemSlot slot, bool locked)
        {
            slot.Locked = locked;
            itemSlots.Dirty();
        }

        /// <summary>
        ///     Update the locked state of the managed item slots.
        /// </summary>
        /// <remarks>
        ///     Note that the slot's ContainerSlot performs its own networking, so we don't need to send information
        ///     about the contained entity.
        /// </remarks>
        private void HandleItemSlotsState(EntityUid uid, ItemSlotsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ItemSlotsComponentState state)
                return;

            foreach (var (id, locked) in state.SlotLocked)
            {
                component.Slots[id].Locked = locked;
            }
        }

        private void GetItemSlotsState(EntityUid uid, ItemSlotsComponent component, ref ComponentGetState args)
        {
            args.State = new ItemSlotsComponentState(component.Slots);
        }
    }
}

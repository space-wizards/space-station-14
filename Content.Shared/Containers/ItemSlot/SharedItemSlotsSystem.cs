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
    public class SharedItemSlotsSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<SharedItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SharedItemSlotsComponent, InteractHandEvent>(OnInteractHand);

            SubscribeLocalEvent<SharedItemSlotsComponent, GetAlternativeVerbsEvent>(AddEjectVerbs);
            SubscribeLocalEvent<SharedItemSlotsComponent, GetInteractionVerbsEvent>(AddInteractionVerbsVerbs);

            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentGetState>(GetItemSlotsState);
            SubscribeLocalEvent<SharedItemSlotsComponent, ComponentHandleState>(HandleItemSlotsState);
        }

        #region ComponentManagement
        /// <summary>
        ///     Spawn in starting items for any item slots that should have one.
        /// </summary>
        private void OnStartup(EntityUid uid, SharedItemSlotsComponent itemSlots, ComponentStartup args)
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.HasItem || string.IsNullOrEmpty(slot.StartingItem))
                    continue;

                var item = EntityManager.SpawnEntity(slot.StartingItem, itemSlots.Owner.Transform.Coordinates);
                slot.ContainerSlot.Insert(item);
            }
        }

        private void OnShutdown(EntityUid uid, SharedItemSlotsComponent itemSlots, ComponentShutdown args)
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                slot.ContainerSlot.Shutdown();
            }
        }

        /// <summary>
        ///     Given a new item slot, store it in the <see cref="SharedItemSlotsComponent"/> and ensure it has an item
        ///     container.
        /// </summary>
        public void RegisterItemSlot(EntityUid uid, string id, ItemSlot slot)
        {
            var itemSlots = EntityManager.EnsureComponent<SharedItemSlotsComponent>(uid);
            ContainerHelpers.EnsureContainer<ContainerSlot>(itemSlots.Owner, id);
            DebugTools.Assert(!itemSlots.Slots.ContainsKey(id));
            itemSlots.Slots.Add(id, slot);
        }
        #endregion

        #region Interactions
        /// <summary>
        ///     Attempt to take an item from a slot, if any are set to EjectOnInteract.
        /// </summary>
        private void OnInteractHand(EntityUid uid, SharedItemSlotsComponent itemSlots, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.Locked || !slot.EjectOnInteract || slot.Item == null)
                    continue;

                args.Handled = true;
                TryEjectToHands(uid, slot, args.User.Uid);
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
        private void OnInteractUsing(EntityUid uid, SharedItemSlotsComponent itemSlots, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(args.User.Uid, out SharedHandsComponent? hands))
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!CanInsert(args.Used, slot, swap: true))
                    continue;

                // Drop the held item onto the floor. Return if the user cannot drop.
                if (!hands.TryDropEntityToFloor(args.Used))
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
            // This also inherits from the more general ContainerModifiedMessage

            if (slot.InsertSound != null)
                SoundSystem.Play(Filter.Pvs(uid), slot.InsertSound.GetSound(), uid);
        }

        /// <summary>
        ///     Can a given item be inserted into a slot. Unless otherwise specified, this will return false if the slot is
        ///     already filled.
        /// </summary>
        public bool CanInsert(IEntity item, ItemSlot slot, bool swap = false)
        {
            if (slot.Locked)
                return false;

            if (!swap && slot.HasItem)
                return false;

            // check if item allowed in whitelist
            if (slot.Whitelist != null && !slot.Whitelist.IsValid(item))
                return false;

            return slot.ContainerSlot.CanInsert(item);
        }

        /// <summary>
        ///     Tries to insert item into a specific slot.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsert(EntityUid uid, string id, IEntity item, SharedItemSlotsComponent? itemSlots = null)
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
            if (!CanInsert(item, slot))
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
            // This also inherits from the more general ContainerModifiedMessage

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
        public bool TryEject(EntityUid uid, string id, [NotNullWhen(true)] out IEntity? item, SharedItemSlotsComponent? itemSlots = null)
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
        /// <returns>False if the id is not valid, the item slot is locked, or it has no item inserted. True otherwise,
        /// even if the user has no hands.</returns>
        public bool TryEjectToHands(EntityUid uid, ItemSlot slot, EntityUid user)
        {
            if (!TryEject(uid, slot, out var item))
                return false;

            if (EntityManager.TryGetComponent(user, out SharedHandsComponent? hands))
                hands.TryPutInAnyHand(item);

            return true;
        }
        #endregion

        #region Verbs
        private void AddEjectVerbs(EntityUid uid, SharedItemSlotsComponent itemSlots, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // Here, we intentionally do not check the "can pickup" action blocker.
            // If they cannot pick up the item, it will simply be ejected onto the floor.

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
                verb.Text = verbSubject;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => TryEjectToHands(uid, slot, args.User.Uid);

                args.Verbs.Add(verb);
            }
        }

        private void AddInteractionVerbsVerbs(EntityUid uid, SharedItemSlotsComponent itemSlots, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // If there are any slots that eject on left-click, add a "Take <item>" verb.
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!slot.EjectOnInteract || slot.Locked || !slot.HasItem)
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : slot.Item!.Name ?? string.Empty;

                Verb takeVerb = new();
                takeVerb.Text = Loc.GetString("take-item-verb-text", ("subject", verbSubject));
                takeVerb.Act = () => TryEjectToHands(uid, slot, args.User.Uid);
                takeVerb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";
                args.Verbs.Add(takeVerb);
            }

            if (args.Using == null || !_actionBlockerSystem.CanDrop(args.User))
                return;

            // Add verbs to insert the held item into any applicable slots
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!CanInsert(args.Using, slot))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : args.Using.Name ?? string.Empty;

                Verb insertVerb = new();
                if (slot.EjectOnInteract)
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

                insertVerb.Act = () => Insert(uid, slot, args.Using);
                args.Verbs.Add(insertVerb);
            }
        }
        #endregion

        /// <summary>
        ///     Get the contents of some item slot.
        /// </summary>
        public IEntity? GetItem(EntityUid uid, string id, SharedItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return null;

            return itemSlots.Slots.GetValueOrDefault(id)?.Item;
        }

        /// <summary>
        ///     Lock an item slot. This stops items from being inserted into or ejected from this slot.
        /// </summary>
        public void SetLock(EntityUid uid, string id, bool locked, SharedItemSlotsComponent? itemSlots = null)
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
        public void SetLock(SharedItemSlotsComponent itemSlots, ItemSlot slot, bool locked)
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
        private void HandleItemSlotsState(EntityUid uid, SharedItemSlotsComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ItemSlotManagerState state)
                return;

            foreach (var (id, locked) in state.SlotLocked)
            {
                component.Slots[id].Locked = locked;
            }
        }

        private void GetItemSlotsState(EntityUid uid, SharedItemSlotsComponent component, ref ComponentGetState args)
        {
            args.State = new ItemSlotManagerState(component.Slots);
        }
    }
}

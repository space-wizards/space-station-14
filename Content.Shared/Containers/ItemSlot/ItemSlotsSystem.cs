using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Containers.ItemSlots
{
    /// <summary>
    ///     A class that handles interactions related to inserting/ejecting items into/from an item slot.
    /// </summary>
    /// <remarks>
    ///     Note when using popups on entities with many slots with InsertOnInteract, EjectOnInteract or EjectOnUse:
    ///     A single use will try to insert to/eject from every slot and generate a popup for each that fails.
    /// </remarks>
    public sealed partial class ItemSlotsSystem : EntitySystem
    {
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containers = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeLock();

            SubscribeLocalEvent<ItemSlotsComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ItemSlotsComponent, ComponentInit>(Oninitialize);

            SubscribeLocalEvent<ItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ItemSlotsComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ItemSlotsComponent, UseInHandEvent>(OnUseInHand);

            SubscribeLocalEvent<ItemSlotsComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
            SubscribeLocalEvent<ItemSlotsComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerbsVerbs);

            SubscribeLocalEvent<ItemSlotsComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<ItemSlotsComponent, DestructionEventArgs>(OnBreak);

            SubscribeLocalEvent<ItemSlotsComponent, ComponentGetState>(GetItemSlotsState);
            SubscribeLocalEvent<ItemSlotsComponent, ComponentHandleState>(HandleItemSlotsState);

            SubscribeLocalEvent<ItemSlotsComponent, ItemSlotButtonPressedEvent>(HandleButtonPressed);
        }

        #region ComponentManagement
        /// <summary>
        ///     Spawn in starting items for any item slots that should have one.
        /// </summary>
        private void OnMapInit(EntityUid uid, ItemSlotsComponent itemSlots, MapInitEvent args)
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.HasItem || string.IsNullOrEmpty(slot.StartingItem))
                    continue;

                var item = EntityManager.SpawnEntity(slot.StartingItem, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
                if (slot.ContainerSlot != null)
                    _containers.Insert(item, slot.ContainerSlot);
            }
        }

        /// <summary>
        ///     Ensure item slots have containers.
        /// </summary>
        private void Oninitialize(EntityUid uid, ItemSlotsComponent itemSlots, ComponentInit args)
        {
            foreach (var (id, slot) in itemSlots.Slots)
            {
                slot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(uid, id);
            }
        }

        /// <summary>
        ///     Given a new item slot, store it in the <see cref="ItemSlotsComponent"/> and ensure the slot has an item
        ///     container.
        /// </summary>
        public void AddItemSlot(EntityUid uid, string id, ItemSlot slot, ItemSlotsComponent? itemSlots = null)
        {
            itemSlots ??= EnsureComp<ItemSlotsComponent>(uid);
            DebugTools.AssertOwner(uid, itemSlots);

            if (itemSlots.Slots.TryGetValue(id, out var existing))
            {
                if (existing.Local)
                    Log.Error($"Duplicate item slot key. Entity: {EntityManager.GetComponent<MetaDataComponent>(uid).EntityName} ({uid}), key: {id}");
                else
                    // server state takes priority
                    slot.CopyFrom(existing);
            }

            slot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(uid, id);
            itemSlots.Slots[id] = slot;
            Dirty(uid, itemSlots);
        }

        /// <summary>
        ///     Remove an item slot. This should generally be called whenever a component that added a slot is being
        ///     removed.
        /// </summary>
        public void RemoveItemSlot(EntityUid uid, ItemSlot slot, ItemSlotsComponent? itemSlots = null)
        {
            if (Terminating(uid) || slot.ContainerSlot == null)
                return;

            _containers.ShutdownContainer(slot.ContainerSlot);

            // Don't log missing resolves. when an entity has all of its components removed, the ItemSlotsComponent may
            // have been removed before some other component that added an item slot (and is now trying to remove it).
            if (!Resolve(uid, ref itemSlots, logMissing: false))
                return;

            itemSlots.Slots.Remove(slot.ContainerSlot.ID);

            if (itemSlots.Slots.Count == 0)
                EntityManager.RemoveComponent(uid, itemSlots);
            else
                Dirty(uid, itemSlots);
        }

        public bool TryGetSlot(EntityUid uid, string slotId, [NotNullWhen(true)] out ItemSlot? itemSlot, ItemSlotsComponent? component = null)
        {
            itemSlot = null;

            if (!Resolve(uid, ref component))
                return false;

            return component.Slots.TryGetValue(slotId, out itemSlot);
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
                if (!slot.EjectOnInteract || slot.Item == null || !CanEject(uid, args.User, slot, popup: args.User))
                    continue;

                args.Handled = true;
                TryEjectToHands(uid, slot, args.User, true);
                break;
            }
        }

        /// <summary>
        ///     Attempt to eject an item from the first valid item slot.
        /// </summary>
        private void OnUseInHand(EntityUid uid, ItemSlotsComponent itemSlots, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!slot.EjectOnUse || slot.Item == null || !CanEject(uid, args.User, slot, popup: args.User))
                    continue;

                args.Handled = true;
                TryEjectToHands(uid, slot, args.User, true);
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

            if (!EntityManager.TryGetComponent(args.User, out HandsComponent? hands))
                return;

            var slots = new List<ItemSlot>();
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!slot.InsertOnInteract)
                    continue;

                if (!CanInsert(uid, args.Used, args.User, slot, swap: slot.Swap, popup: args.User))
                    continue;

                slots.Add(slot);
            }

            if (slots.Count == 0)
                return;

            // Drop the held item onto the floor. Return if the user cannot drop.
            if (!_handsSystem.TryDrop(args.User, args.Used, handsComp: hands))
                return;

            slots.Sort(SortEmpty);

            foreach (var slot in slots)
            {
                if (slot.Item != null)
                    _handsSystem.TryPickupAnyHand(args.User, slot.Item.Value, handsComp: hands);

                Insert(uid, slot, args.Used, args.User, excludeUserAudio: true);

                if (slot.InsertSuccessPopup.HasValue)
                    _popupSystem.PopupClient(Loc.GetString(slot.InsertSuccessPopup), uid, args.User);

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
        /// <param name="excludeUserAudio">If true, will exclude the user when playing sound. Does nothing client-side.
        /// Useful for predicted interactions</param>
        private void Insert(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
        {
            bool? inserted = slot.ContainerSlot != null ? _containers.Insert(item, slot.ContainerSlot) : null;
            // ContainerSlot automatically raises a directed EntInsertedIntoContainerMessage

            // Logging
            if (inserted != null && inserted.Value && user != null)
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value)} inserted {ToPrettyString(item)} into {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");

            _audioSystem.PlayPredicted(slot.InsertSound, uid, excludeUserAudio ? user : null);
        }

        /// <summary>
        ///     Check whether a given item can be inserted into a slot. Unless otherwise specified, this will return
        ///     false if the slot is already filled.
        /// </summary>
        /// <remarks>
        ///     If a popup entity is given, and if the item slot is set to generate a popup message when it fails to
        ///     pass the whitelist or due to slot being locked, then this will generate an appropriate popup.
        /// </remarks>
        public bool CanInsert(EntityUid uid, EntityUid usedUid, EntityUid? user, ItemSlot slot, bool swap = false, EntityUid? popup = null)
        {
            if (slot.ContainerSlot == null)
                return false;

            if (_whitelistSystem.IsWhitelistFail(slot.Whitelist, usedUid) || _whitelistSystem.IsBlacklistPass(slot.Blacklist, usedUid))
            {
                if (popup.HasValue && slot.WhitelistFailPopup.HasValue)
                    _popupSystem.PopupClient(Loc.GetString(slot.WhitelistFailPopup), uid, popup.Value);
                return false;
            }

            if (slot.Locked)
            {
                if (popup.HasValue && slot.LockedFailPopup.HasValue)
                    _popupSystem.PopupClient(Loc.GetString(slot.LockedFailPopup), uid, popup.Value);
                return false;
            }

            if (slot.HasItem && (!swap || (swap && !CanEject(uid, user, slot))))
                return false;

            var ev = new ItemSlotInsertAttemptEvent(uid, usedUid, user, slot);
            RaiseLocalEvent(uid, ref ev);
            RaiseLocalEvent(usedUid, ref ev);
            if (ev.Cancelled)
                return false;

            return _containers.CanInsert(usedUid, slot.ContainerSlot, assumeEmpty: swap);
        }

        /// <summary>
        ///     Tries to insert item into a specific slot.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsert(EntityUid uid, string id, EntityUid item, EntityUid? user, ItemSlotsComponent? itemSlots = null, bool excludeUserAudio = false)
        {
            if (!Resolve(uid, ref itemSlots))
                return false;

            if (!itemSlots.Slots.TryGetValue(id, out var slot))
                return false;

            return TryInsert(uid, slot, item, user, excludeUserAudio: excludeUserAudio);
        }

        /// <summary>
        ///     Tries to insert item into a specific slot.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsert(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
        {
            if (!CanInsert(uid, item, user, slot))
                return false;

            Insert(uid, slot, item, user, excludeUserAudio: excludeUserAudio);
            return true;
        }

        /// <summary>
        ///     Tries to insert item into a specific slot from an entity's hand.
        ///     Does not check action blockers.
        /// </summary>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertFromHand(EntityUid uid, ItemSlot slot, EntityUid user, HandsComponent? hands = null, bool excludeUserAudio = false)
        {
            if (!Resolve(user, ref hands, false))
                return false;

            if (hands.ActiveHand?.HeldEntity is not { } held)
                return false;

            if (!CanInsert(uid, held, user, slot))
                return false;

            // hands.Drop(item) checks CanDrop action blocker
            if (!_handsSystem.TryDrop(user, hands.ActiveHand))
                return false;

            Insert(uid, slot, held, user, excludeUserAudio: excludeUserAudio);
            return true;
        }

        /// <summary>
        ///     Tries to insert an item into any empty slot.
        /// </summary>
        /// <param name="ent">The entity that has the item slots.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <param name="user">The entity performing the interaction.</param>
        /// <param name="excludeUserAudio">
        ///     If true, will exclude the user when playing sound. Does nothing client-side.
        ///     Useful for predicted interactions
        /// </param>
        /// <returns>False if failed to insert item</returns>
        public bool TryInsertEmpty(Entity<ItemSlotsComponent?> ent, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return false;

            var slots = new List<ItemSlot>();
            foreach (var slot in ent.Comp.Slots.Values)
            {
                if (slot.ContainerSlot?.ContainedEntity != null)
                    continue;

                if (CanInsert(ent, item, user, slot))
                    slots.Add(slot);
            }

            if (slots.Count == 0)
                return false;

            if (user != null && _handsSystem.IsHolding(user.Value, item))
            {
                if (!_handsSystem.TryDrop(user.Value, item))
                    return false;
            }

            slots.Sort(SortEmpty);

            foreach (var slot in slots)
            {
                if (TryInsert(ent, slot, item, user, excludeUserAudio: excludeUserAudio))
                    return true;
            }

            return false;
        }

        private static int SortEmpty(ItemSlot a, ItemSlot b)
        {
            var aEnt = a.ContainerSlot?.ContainedEntity;
            var bEnt = b.ContainerSlot?.ContainedEntity;
            if (aEnt == null && bEnt == null)
                return a.Priority.CompareTo(b.Priority);

            if (aEnt == null)
                return -1;

            return 1;
        }
        #endregion

        #region Eject

        /// <summary>
        ///     Check whether an ejection from a given slot may happen.
        /// </summary>
        /// <remarks>
        ///     If a popup entity is given, this will generate a popup message if any are configured on the the item slot.
        /// </remarks>
        public bool CanEject(EntityUid uid, EntityUid? user, ItemSlot slot, EntityUid? popup = null)
        {
            if (slot.Locked)
            {
                if (popup.HasValue && slot.LockedFailPopup.HasValue)
                    _popupSystem.PopupClient(Loc.GetString(slot.LockedFailPopup), uid, popup.Value);
                return false;
            }

            if (slot.ContainerSlot?.ContainedEntity is not {} item)
                return false;

            var ev = new ItemSlotEjectAttemptEvent(uid, item, user, slot);
            RaiseLocalEvent(uid, ref ev);
            RaiseLocalEvent(item, ref ev);
            if (ev.Cancelled)
                return false;

            return _containers.CanRemove(item, slot.ContainerSlot);
        }

        /// <summary>
        ///     Eject an item from a slot. This does not perform checks (e.g., is the slot locked?), so you should
        ///     probably just use <see cref="TryEject"/> instead.
        /// </summary>
        /// <param name="excludeUserAudio">If true, will exclude the user when playing sound. Does nothing client-side.
        /// Useful for predicted interactions</param>
        private void Eject(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
        {
            bool? ejected = slot.ContainerSlot != null ? _containers.Remove(item, slot.ContainerSlot) : null;
            // ContainerSlot automatically raises a directed EntRemovedFromContainerMessage

            // Logging
            if (ejected != null && ejected.Value && user != null)
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value)} ejected {ToPrettyString(item)} from {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");

            _audioSystem.PlayPredicted(slot.EjectSound, uid, excludeUserAudio ? user : null);
        }

        /// <summary>
        ///     Try to eject an item from a slot.
        /// </summary>
        /// <returns>False if item slot is locked or has no item inserted</returns>
        public bool TryEject(EntityUid uid, ItemSlot slot, EntityUid? user, [NotNullWhen(true)] out EntityUid? item, bool excludeUserAudio = false)
        {
            item = null;

            // This handles logic with the slot itself
            if (!CanEject(uid, user, slot))
                return false;

            item = slot.Item;

            // This handles user logic
            if (user != null && item != null && !_actionBlockerSystem.CanPickup(user.Value, item.Value))
                return false;

            Eject(uid, slot, item!.Value, user, excludeUserAudio);
            return true;
        }

        /// <summary>
        ///     Try to eject item from a slot.
        /// </summary>
        /// <returns>False if the id is not valid, the item slot is locked, or it has no item inserted</returns>
        public bool TryEject(EntityUid uid, string id, EntityUid? user,
            [NotNullWhen(true)] out EntityUid? item, ItemSlotsComponent? itemSlots = null, bool excludeUserAudio = false)
        {
            item = null;

            if (!Resolve(uid, ref itemSlots))
                return false;

            if (!itemSlots.Slots.TryGetValue(id, out var slot))
                return false;

            return TryEject(uid, slot, user, out item, excludeUserAudio);
        }

        /// <summary>
        ///     Try to eject item from a slot directly into a user's hands. If they have no hands, the item will still
        ///     be ejected onto the floor.
        /// </summary>
        /// <returns>
        ///     False if the id is not valid, the item slot is locked, or it has no item inserted. True otherwise, even
        ///     if the user has no hands.
        /// </returns>
        public bool TryEjectToHands(EntityUid uid, ItemSlot slot, EntityUid? user, bool excludeUserAudio = false)
        {
            if (!TryEject(uid, slot, user, out var item, excludeUserAudio))
                return false;

            if (user != null)
                _handsSystem.PickupOrDrop(user.Value, item.Value);

            return true;
        }
        #endregion

        #region Verbs
        private void AddAlternativeVerbs(EntityUid uid, ItemSlotsComponent itemSlots, GetVerbsEvent<AlternativeVerb> args)
        {
            if (args.Hands == null || !args.CanAccess ||!args.CanInteract)
            {
                return;
            }

            // Add the insert-item verbs
            if (args.Using != null && _actionBlockerSystem.CanDrop(args.User))
            {
                var canInsertAny = false;
                foreach (var slot in itemSlots.Slots.Values)
                {
                    // Disable slot insert if InsertOnInteract is true
                    if (slot.InsertOnInteract || !CanInsert(uid, args.Using.Value, args.User, slot))
                        continue;

                    var verbSubject = slot.Name != string.Empty
                        ? Loc.GetString(slot.Name)
                        : Name(args.Using.Value);

                    AlternativeVerb verb = new()
                    {
                        IconEntity = GetNetEntity(args.Using),
                        Act = () => Insert(uid, slot, args.Using.Value, args.User, excludeUserAudio: true)
                    };

                    if (slot.InsertVerbText != null)
                    {
                        verb.Text = Loc.GetString(slot.InsertVerbText);
                        verb.Icon = new SpriteSpecifier.Texture(
                            new("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));
                    }
                    else if (slot.EjectOnInteract)
                    {
                        // Inserting/ejecting is a primary interaction for this entity. Instead of using the insert
                        // category, we will use a single "Place <item>" verb.
                        verb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
                        verb.Icon = new SpriteSpecifier.Texture(
                            new("/Textures/Interface/VerbIcons/drop.svg.192dpi.png"));
                    }
                    else
                    {
                        verb.Category = VerbCategory.Insert;
                        verb.Text = verbSubject;
                    }

                    verb.Priority = slot.Priority;
                    args.Verbs.Add(verb);
                    canInsertAny = true;
                }

                // If can insert then insert. Don't run eject verbs.
                if (canInsertAny)
                    return;
            }

            // Add the eject-item verbs
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.EjectOnInteract || slot.DisableEject)
                    // For this item slot, ejecting/inserting is a primary interaction. Instead of an eject category
                    // alt-click verb, there will be a "Take item" primary interaction verb.
                    continue;

                if (!CanEject(uid, args.User, slot))
                    continue;

                if (!_actionBlockerSystem.CanPickup(args.User, slot.Item!.Value))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : EntityManager.GetComponent<MetaDataComponent>(slot.Item.Value).EntityName ?? string.Empty;

                AlternativeVerb verb = new()
                {
                    IconEntity = GetNetEntity(slot.Item),
                    Act = () => TryEjectToHands(uid, slot, args.User, excludeUserAudio: true)
                };

                if (slot.EjectVerbText == null)
                {
                    verb.Text = verbSubject;
                    verb.Category = VerbCategory.Eject;
                }
                else
                {
                    verb.Text = Loc.GetString(slot.EjectVerbText);
                }

                verb.Priority = slot.Priority;
                args.Verbs.Add(verb);
            }
        }

        private void AddInteractionVerbsVerbs(EntityUid uid, ItemSlotsComponent itemSlots, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // If there are any slots that eject on left-click, add a "Take <item>" verb.
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!slot.EjectOnInteract || !CanEject(uid, args.User, slot))
                    continue;

                if (!_actionBlockerSystem.CanPickup(args.User, slot.Item!.Value))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : Name(slot.Item!.Value);

                InteractionVerb takeVerb = new()
                {
                    IconEntity = GetNetEntity(slot.Item),
                    Act = () => TryEjectToHands(uid, slot, args.User, excludeUserAudio: true)
                };

                if (slot.EjectVerbText == null)
                    takeVerb.Text = Loc.GetString("take-item-verb-text", ("subject", verbSubject));
                else
                    takeVerb.Text = Loc.GetString(slot.EjectVerbText);

                takeVerb.Priority = slot.Priority;
                args.Verbs.Add(takeVerb);
            }

            // Next, add the insert-item verbs
            if (args.Using == null || !_actionBlockerSystem.CanDrop(args.User))
                return;

            foreach (var slot in itemSlots.Slots.Values)
            {
                if (!slot.InsertOnInteract || !CanInsert(uid, args.Using.Value, args.User, slot))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : Name(args.Using.Value);

                InteractionVerb insertVerb = new()
                {
                    IconEntity = GetNetEntity(args.Using),
                    Act = () => Insert(uid, slot, args.Using.Value, args.User, excludeUserAudio: true)
                };

                if (slot.InsertVerbText != null)
                {
                    insertVerb.Text = Loc.GetString(slot.InsertVerbText);
                    insertVerb.Icon =
                        new SpriteSpecifier.Texture(
                            new ResPath("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));
                }
                else if(slot.EjectOnInteract)
                {
                    // Inserting/ejecting is a primary interaction for this entity. Instead of using the insert
                    // category, we will use a single "Place <item>" verb.
                    insertVerb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
                    insertVerb.Icon =
                        new SpriteSpecifier.Texture(
                            new ResPath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png"));
                }
                else
                {
                    insertVerb.Category = VerbCategory.Insert;
                    insertVerb.Text = verbSubject;
                }

                insertVerb.Priority = slot.Priority;
                args.Verbs.Add(insertVerb);
            }
        }
        #endregion

        #region BUIs
        private void HandleButtonPressed(EntityUid uid, ItemSlotsComponent component, ItemSlotButtonPressedEvent args)
        {
            if (!component.Slots.TryGetValue(args.SlotId, out var slot))
                return;

            if (args.TryEject && slot.HasItem)
                TryEjectToHands(uid, slot, args.Actor, true);
            else if (args.TryInsert && !slot.HasItem)
                TryInsertFromHand(uid, slot, args.Actor);
        }
        #endregion

        /// <summary>
        ///     Eject items from (some) slots when the entity is destroyed.
        /// </summary>
        private void OnBreak(EntityUid uid, ItemSlotsComponent component, EntityEventArgs args)
        {
            foreach (var slot in component.Slots.Values)
            {
                if (slot.EjectOnBreak && slot.HasItem)
                {
                    SetLock(uid, slot, false, component);
                    TryEject(uid, slot, null, out var _);
                }
            }
        }

        /// <summary>
        ///     Get the contents of some item slot.
        /// </summary>
        /// <returns>The item in the slot, or null if the slot is empty or the entity doesn't have an <see cref="ItemSlotsComponent"/>.</returns>
        public EntityUid? GetItemOrNull(EntityUid uid, string id, ItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots, logMissing: false))
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

            SetLock(uid, slot, locked, itemSlots);
        }

        /// <summary>
        ///     Lock an item slot. This stops items from being inserted into or ejected from this slot.
        /// </summary>
        public void SetLock(EntityUid uid, ItemSlot slot, bool locked, ItemSlotsComponent? itemSlots = null)
        {
            if (!Resolve(uid, ref itemSlots))
                return;

            slot.Locked = locked;
            Dirty(uid, itemSlots);
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

            foreach (var (key, slot) in component.Slots)
            {
                if (!state.Slots.ContainsKey(key))
                    RemoveItemSlot(uid, slot, component);
            }

            foreach (var (serverKey, serverSlot) in state.Slots)
            {
                if (component.Slots.TryGetValue(serverKey, out var itemSlot))
                {
                    itemSlot.CopyFrom(serverSlot);
                    itemSlot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(uid, serverKey);
                }
                else
                {
                    var slot = new ItemSlot(serverSlot);
                    slot.Local = false;
                    AddItemSlot(uid, serverKey, slot);
                }
            }
        }

        private void GetItemSlotsState(EntityUid uid, ItemSlotsComponent component, ref ComponentGetState args)
        {
            args.State = new ItemSlotsComponentState(component.Slots);
        }
    }
}

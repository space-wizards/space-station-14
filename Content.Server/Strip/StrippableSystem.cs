using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Ensnaring;
using Content.Server.Hands.Components;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;
using Content.Shared.Strip;
using Robust.Shared.Utility;

namespace Content.Server.Strip
{
    public sealed class StrippableSystem : SharedStrippableSystem
    {
        [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly EnsnareableSystem _ensnaring = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        // TODO: ECS popups. Not all of these have ECS equivalents yet.

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrippableComponent, GetVerbsEvent<Verb>>(AddStripVerb);
            SubscribeLocalEvent<StrippableComponent, GetVerbsEvent<ExamineVerb>>(AddStripExamineVerb);
            SubscribeLocalEvent<StrippableComponent, ActivateInWorldEvent>(OnActivateInWorld);

            // BUI
            SubscribeLocalEvent<StrippableComponent, StrippingSlotButtonPressed>(OnStripButtonPressed);
            SubscribeLocalEvent<EnsnareableComponent, StrippingEnsnareButtonPressed>(OnStripEnsnareMessage);
        }

        private void OnStripEnsnareMessage(EntityUid uid, EnsnareableComponent component, StrippingEnsnareButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user)
                return;

            foreach (var entity in component.Container.ContainedEntities)
            {
                if (!TryComp<EnsnaringComponent>(entity, out var ensnaring))
                    continue;

                _ensnaring.TryFree(uid, entity, ensnaring, user);
                return;
            }
        }

        private void OnStripButtonPressed(EntityUid uid, StrippableComponent component, StrippingSlotButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user ||
                !TryComp<HandsComponent>(user, out var userHands))
                return;

            if (args.IsHand)
            {
                StripHand(uid, user, args.Slot, component,  userHands);
                return;
            }

            if (!TryComp<InventoryComponent>(component.Owner, out var inventory))
                return;

            var hasEnt = _inventorySystem.TryGetSlotEntity(component.Owner, args.Slot, out _, inventory);

            if (userHands.ActiveHandEntity != null && !hasEnt)
                PlaceActiveHandItemInInventory(user, args.Slot, component);
            else if (userHands.ActiveHandEntity == null && hasEnt)
                TakeItemFromInventory(user, args.Slot, component);
        }

        private void StripHand(EntityUid target, EntityUid user, string handId, StrippableComponent component, HandsComponent userHands)
        {
            if (!TryComp<HandsComponent>(target, out var targetHands)
                || !targetHands.Hands.TryGetValue(handId, out var hand))
                return;

            // is the target a handcuff?
            if (TryComp(hand.HeldEntity, out HandVirtualItemComponent? virt)
                && TryComp(target, out CuffableComponent? cuff)
                && _cuffable.GetAllCuffs(cuff).Contains(virt.BlockingEntity))
            {
                _cuffable.TryUncuff(target, user, virt.BlockingEntity, cuffable: cuff);
                return;
            }

            if (hand.IsEmpty && userHands.ActiveHandEntity != null)
                PlaceActiveHandItemInHands(user, handId, component);
            else if (!hand.IsEmpty && userHands.ActiveHandEntity == null)
                TakeItemFromHands(user, handId, component);
        }

        public override void StartOpeningStripper(EntityUid user, StrippableComponent component, bool openInCombat = false)
        {
            base.StartOpeningStripper(user, component, openInCombat);

            if (TryComp<SharedCombatModeComponent>(user, out var mode) && mode.IsInCombatMode && !openInCombat)
                return;

            if (TryComp<ActorComponent>(user, out var actor))
            {
                if (_userInterfaceSystem.SessionHasOpenUi(component.Owner, StrippingUiKey.Key, actor.PlayerSession))
                    return;
                _userInterfaceSystem.TryOpen(component.Owner, StrippingUiKey.Key, actor.PlayerSession);
            }
        }

        private void AddStripVerb(EntityUid uid, StrippableComponent component, GetVerbsEvent<Verb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            Verb verb = new()
            {
                Text = Loc.GetString("strip-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Act = () => StartOpeningStripper(args.User, component, true),
            };
            args.Verbs.Add(verb);
        }

        private void AddStripExamineVerb(EntityUid uid, StrippableComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
                return;

            if (!HasComp<ActorComponent>(args.User))
                return;

            ExamineVerb verb = new()
            {
                Text = Loc.GetString("strip-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Act = () => StartOpeningStripper(args.User, component, true),
                Category = VerbCategory.Examine,
            };

            args.Verbs.Add(verb);
        }

        private void OnActivateInWorld(EntityUid uid, StrippableComponent component, ActivateInWorldEvent args)
        {
            if (args.Target == args.User)
                return;

            if (!HasComp<ActorComponent>(args.User))
                return;

            args.Handled = true;
            StartOpeningStripper(args.User, component);
        }

        /// <summary>
        ///     Places item in user's active hand to an inventory slot.
        /// </summary>
        private async void PlaceActiveHandItemInInventory(EntityUid user, string slot, StrippableComponent component)
        {
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (userHands.ActiveHand?.HeldEntity is not { } held)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!_handsSystem.CanDropHeld(user, userHands.ActiveHand))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop"));
                    return false;
                }

                if (!_inventorySystem.HasSlot(component.Owner, slot))
                    return false;

                if (_inventorySystem.TryGetSlotEntity(component.Owner, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-occupied",("owner", component.Owner)));
                    return false;
                }

                if (!_inventorySystem.CanEquip(user, component.Owner, held, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-equip-message",("owner", component.Owner)));
                    return false;
                }

                return true;
            }

            if (!_inventorySystem.TryGetSlot(component.Owner, slot, out var slotDef))
            {
                Logger.Error($"{ToPrettyString(user)} attempted to place an item in a non-existent inventory slot ({slot}) on {ToPrettyString(component.Owner)}");
                return;
            }

            var (time, stealth) = GetStripTimeModifiers(user, component.Owner, slotDef.StripTime);

            var doAfterArgs = new DoAfterEventArgs(user, time, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (!stealth && Check() && userHands.ActiveHandEntity != null)
            {
                var message = Loc.GetString("strippable-component-alert-owner-insert",
                    ("user", Identity.Entity(user, EntityManager)), ("item", userHands.ActiveHandEntity));
                _popupSystem.PopupEntity(message, component.Owner, component.Owner, PopupType.Large);
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHand?.HeldEntity is { } held
                && _handsSystem.TryDrop(user, userHands.ActiveHand, handsComp: userHands))
            {
                _inventorySystem.TryEquip(user, component.Owner, held, slot);

                _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has placed the item {ToPrettyString(held):item} in {ToPrettyString(component.Owner):target}'s {slot} slot");
            }
        }

        /// <summary>
        ///     Places item in user's active hand in one of the entity's hands.
        /// </summary>
        private async void PlaceActiveHandItemInHands(EntityUid user, string handName, StrippableComponent component)
        {
            var hands = Comp<HandsComponent>(component.Owner);
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (userHands.ActiveHandEntity == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!_handsSystem.CanDropHeld(user, userHands.ActiveHand!))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop"));
                    return false;
                }

                if (!hands.Hands.TryGetValue(handName, out var hand)
                    || !_handsSystem.CanPickupToHand(component.Owner, userHands.ActiveHandEntity.Value, hand, checkActionBlocker: false, hands))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-put-message",("owner", component.Owner)));
                    return false;
                }

                return true;
            }

            var (time, stealth) = GetStripTimeModifiers(user, component.Owner, component.HandStripDelay);

            var doAfterArgs = new DoAfterEventArgs(user, time, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (!stealth
                && Check()
                && userHands.Hands.TryGetValue(handName, out var handSlot)
                && handSlot.HeldEntity != null)
            {
                    _popupSystem.PopupEntity(
                        Loc.GetString("strippable-component-alert-owner-insert",
                        ("user", Identity.Entity(user, EntityManager)),
                        ("item", handSlot.HeldEntity)),
                        component.Owner, component.Owner, PopupType.Large);
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHandEntity is not { } held)
                return;

            _handsSystem.TryDrop(user, checkActionBlocker: false, handsComp: userHands);
            _handsSystem.TryPickup(component.Owner, held, handName, checkActionBlocker: false, animateUser: true, animate: !stealth, handsComp: hands);
            _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has placed the item {ToPrettyString(held):item} in {ToPrettyString(component.Owner):target}'s hands");
            // hand update will trigger strippable update
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(EntityUid user, string slot, StrippableComponent component)
        {
            bool Check()
            {
                if (!_inventorySystem.HasSlot(component.Owner, slot))
                    return false;

                if (!_inventorySystem.TryGetSlotEntity(component.Owner, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-free-message", ("owner", component.Owner)));
                    return false;
                }

                if (!_inventorySystem.CanUnequip(user, component.Owner, slot, out var reason))
                {
                    user.PopupMessageCursor(reason);
                    return false;
                }

                return true;
            }

            if (!_inventorySystem.TryGetSlot(component.Owner, slot, out var slotDef))
            {
                Logger.Error($"{ToPrettyString(user)} attempted to take an item from a non-existent inventory slot ({slot}) on {ToPrettyString(component.Owner)}");
                return;
            }

            var (time, stealth) = GetStripTimeModifiers(user, component.Owner, slotDef.StripTime);

            var doAfterArgs = new DoAfterEventArgs(user, time, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            if (!stealth && Check())
            {
                if (slotDef.StripHidden)
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner-hidden", ("slot", slot)), component.Owner,
                        component.Owner, PopupType.Large);
                }
                else if (_inventorySystem.TryGetSlotEntity(component.Owner, slot, out var slotItem))
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner", ("user", Identity.Entity(user, EntityManager)), ("item", slotItem)), component.Owner,
                        component.Owner, PopupType.Large);
                }
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (_inventorySystem.TryGetSlotEntity(component.Owner, slot, out var item) && _inventorySystem.TryUnequip(user, component.Owner, slot))
            {
                // Raise a dropped event, so that things like gas tank internals properly deactivate when stripping
                RaiseLocalEvent(item.Value, new DroppedEvent(user), true);

                _handsSystem.PickupOrDrop(user, item.Value, animate: !stealth);
                _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has stripped the item {ToPrettyString(item.Value):item} from {ToPrettyString(component.Owner):target}");
            }
        }

        /// <summary>
        ///     Takes an item from a hand and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromHands(EntityUid user, string handName, StrippableComponent component)
        {
            var hands = Comp<HandsComponent>(component.Owner);
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (!hands.Hands.TryGetValue(handName, out var hand) || hand.HeldEntity == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-free-message",("owner", component.Owner)));
                    return false;
                }

                if (HasComp<HandVirtualItemComponent>(hand.HeldEntity))
                    return false;

                if (!_handsSystem.CanDropHeld(component.Owner, hand, false))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop-message",("owner", component.Owner)));
                    return false;
                }

                return true;
            }

            var (time, stealth) = GetStripTimeModifiers(user, component.Owner, component.HandStripDelay);

            var doAfterArgs = new DoAfterEventArgs(user, time, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            if (!stealth
                && Check()
                && hands.Hands.TryGetValue(handName, out var handSlot)
                && handSlot.HeldEntity != null)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("strippable-component-alert-owner",
                    ("user", Identity.Entity(user, EntityManager)),
                    ("item", handSlot.HeldEntity)),
                    component.Owner, component.Owner);
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (!hands.Hands.TryGetValue(handName, out var hand) || hand.HeldEntity is not { } held)
                return;

            _handsSystem.TryDrop(component.Owner, hand, checkActionBlocker: false, handsComp: hands);
            _handsSystem.PickupOrDrop(user, held, handsComp: userHands, animate: !stealth);
            // hand update will trigger strippable update
            _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has stripped the item {ToPrettyString(held):item} from {ToPrettyString(component.Owner):target}");
        }
    }
}

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Ensnaring;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Strip
{
    public sealed class StrippableSystem : SharedStrippableSystem
    {
        [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
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

                _ensnaring.TryFree(uid, user, entity, ensnaring);
                return;
            }
        }

        private void OnStripButtonPressed(Entity<StrippableComponent> strippable, ref StrippingSlotButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user ||
                !TryComp<HandsComponent>(user, out var userHands))
                return;

            if (args.IsHand)
            {
                StripHand(user, args.Slot, strippable, userHands);
                return;
            }

            if (!TryComp<InventoryComponent>(strippable, out var inventory))
                return;

            var hasEnt = _inventorySystem.TryGetSlotEntity(strippable, args.Slot, out var held, inventory);

            if (userHands.ActiveHandEntity != null && !hasEnt)
                PlaceActiveHandItemInInventory(user, strippable, userHands.ActiveHandEntity.Value, args.Slot, strippable);
            else if (userHands.ActiveHandEntity == null && hasEnt)
                TakeItemFromInventory(user, strippable, held!.Value, args.Slot, strippable);
        }

        private void StripHand(EntityUid user, string handId, Entity<StrippableComponent> target, HandsComponent userHands)
        {
            if (!_handsSystem.TryGetHand(target, handId, out var hand))
                return;

            // is the target a handcuff?
            if (TryComp(hand.HeldEntity, out HandVirtualItemComponent? virt)
                && TryComp(target, out CuffableComponent? cuff)
                && _cuffable.GetAllCuffs(cuff).Contains(virt.BlockingEntity))
            {
                _cuffable.TryUncuff(target, user, virt.BlockingEntity, cuffable: cuff);
                return;
            }

            if (userHands.ActiveHandEntity != null && hand.HeldEntity == null)
                PlaceActiveHandItemInHands(user, target, userHands.ActiveHandEntity.Value, handId, target);
            else if (userHands.ActiveHandEntity == null && hand.HeldEntity != null)
                TakeItemFromHands(user, target, hand.HeldEntity.Value, handId, target);
        }

        public override void StartOpeningStripper(EntityUid user, Entity<StrippableComponent> strippable, bool openInCombat = false)
        {
            base.StartOpeningStripper(user, strippable, openInCombat);

            if (TryComp<CombatModeComponent>(user, out var mode) && mode.IsInCombatMode && !openInCombat)
                return;

            if (TryComp<ActorComponent>(user, out var actor))
            {
                if (_userInterfaceSystem.SessionHasOpenUi(strippable, StrippingUiKey.Key, actor.PlayerSession))
                    return;
                _userInterfaceSystem.TryOpen(strippable, StrippingUiKey.Key, actor.PlayerSession);
            }
        }

        private void AddStripVerb(EntityUid uid, StrippableComponent component, GetVerbsEvent<Verb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
                return;

            if (!HasComp<ActorComponent>(args.User))
                return;

            Verb verb = new()
            {
                Text = Loc.GetString("strip-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Act = () => StartOpeningStripper(args.User, (uid, component), true),
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
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
                Act = () => StartOpeningStripper(args.User, (uid, component), true),
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

            StartOpeningStripper(args.User, (uid, component));
        }

        /// <summary>
        ///     Places item in user's active hand to an inventory slot.
        /// </summary>
        private async void PlaceActiveHandItemInInventory(
            EntityUid user,
            EntityUid target,
            EntityUid held,
            string slot,
            StrippableComponent component)
        {
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (userHands.ActiveHandEntity != held)
                    return false;

                if (!_handsSystem.CanDropHeld(user, userHands.ActiveHand!))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-cannot-drop"), user);
                    return false;
                }

                if (_inventorySystem.TryGetSlotEntity(target, slot, out _))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-item-slot-occupied",("owner", target)), user);
                    return false;
                }

                if (!_inventorySystem.CanEquip(user, target, held, slot, out _))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-cannot-equip-message",("owner", target)), user);
                    return false;
                }

                return true;
            }

            if (!_inventorySystem.TryGetSlot(target, slot, out var slotDef))
            {
                Logger.Error($"{ToPrettyString(user)} attempted to place an item in a non-existent inventory slot ({slot}) on {ToPrettyString(target)}");
                return;
            }

            var userEv = new BeforeStripEvent(slotDef.StripTime);
            RaiseLocalEvent(user, userEv);
            var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
            RaiseLocalEvent(target, ev);

            var doAfterArgs = new DoAfterArgs(EntityManager, user, ev.Time, new AwaitedDoAfterEvent(), null, target: target, used: held)
            {
                ExtraCheck = Check,
                Hidden = ev.Stealth,
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool // Block any other DoAfters featuring this same entity.
            };

            if (!ev.Stealth && Check() && userHands.ActiveHandEntity != null)
            {
                var message = Loc.GetString("strippable-component-alert-owner-insert",
                    ("user", Identity.Entity(user, EntityManager)), ("item", userHands.ActiveHandEntity));
                _popup.PopupEntity(message, target, target, PopupType.Large);
            }

            _adminLogger.Add(LogType.Stripping, LogImpact.Low, $"{ToPrettyString(user):user} is trying to place the item {ToPrettyString(held):item} in {ToPrettyString(target):target}'s {slot} slot");

            var result = await _doAfter.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished)
                return;

            DebugTools.Assert(userHands.ActiveHand?.HeldEntity == held);

            if (_handsSystem.TryDrop(user, handsComp: userHands))
            {
                _inventorySystem.TryEquip(user, target, held, slot);

                _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has placed the item {ToPrettyString(held):item} in {ToPrettyString(target):target}'s {slot} slot");
            }
        }

        /// <summary>
        ///     Places item in user's active hand in one of the entity's hands.
        /// </summary>
        private async void PlaceActiveHandItemInHands(
            EntityUid user,
            EntityUid target,
            EntityUid held,
            string handName,
            StrippableComponent component)
        {
            var hands = Comp<HandsComponent>(target);
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (userHands.ActiveHandEntity != held)
                    return false;

                if (!_handsSystem.CanDropHeld(user, userHands.ActiveHand!))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-cannot-drop"), user);
                    return false;
                }

                if (!_handsSystem.TryGetHand(target, handName, out var hand, hands)
                    || !_handsSystem.CanPickupToHand(target, userHands.ActiveHandEntity.Value, hand, checkActionBlocker: false, hands))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-cannot-put-message",("owner", target)), user);
                    return false;
                }

                return true;
            }

            var userEv = new BeforeStripEvent(component.HandStripDelay);
            RaiseLocalEvent(user, userEv);
            var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
            RaiseLocalEvent(target, ev);

            var doAfterArgs = new DoAfterArgs(EntityManager, user, ev.Time, new AwaitedDoAfterEvent(), null, target: target, used: held)
            {
                ExtraCheck = Check,
                Hidden = ev.Stealth,
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            _adminLogger.Add(LogType.Stripping, LogImpact.Low, $"{ToPrettyString(user):user} is trying to place the item {ToPrettyString(held):item} in {ToPrettyString(target):target}'s hands");

            var result = await _doAfter.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            _handsSystem.TryDrop(user, checkActionBlocker: false, handsComp: userHands);
            _handsSystem.TryPickup(target, held, handName, checkActionBlocker: false, animateUser: !ev.Stealth, animate: !ev.Stealth, handsComp: hands);
            _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has placed the item {ToPrettyString(held):item} in {ToPrettyString(target):target}'s hands");
            // hand update will trigger strippable update
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(
            EntityUid user,
            EntityUid target,
            EntityUid item,
            string slot,
            Entity<StrippableComponent> strippable)
        {
            bool Check()
            {
                if (!_inventorySystem.TryGetSlotEntity(target, slot, out var ent) && ent == item)
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-item-slot-free-message", ("owner", target)), user);
                    return false;
                }

                if (!_inventorySystem.CanUnequip(user, target, slot, out var reason))
                {
                    _popup.PopupCursor(Loc.GetString(reason), user);
                    return false;
                }

                return true;
            }

            if (!_inventorySystem.TryGetSlot(target, slot, out var slotDef))
            {
                Logger.Error($"{ToPrettyString(user)} attempted to take an item from a non-existent inventory slot ({slot}) on {ToPrettyString(target)}");
                return;
            }

            var userEv = new BeforeStripEvent(slotDef.StripTime);
            RaiseLocalEvent(user, userEv);
            var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
            RaiseLocalEvent(target, ev);

            var doAfterArgs = new DoAfterArgs(EntityManager, user, ev.Time, new AwaitedDoAfterEvent(), null, target: target, used: item)
            {
                ExtraCheck = Check,
                Hidden = ev.Stealth,
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                BreakOnHandChange = false, // allow simultaneously removing multiple items.
                DuplicateCondition = DuplicateConditions.SameTool
            };

            if (!ev.Stealth && Check())
            {
                if (slotDef.StripHidden)
                {
                    _popup.PopupEntity(Loc.GetString("strippable-component-alert-owner-hidden", ("slot", slot)), target,
                        target, PopupType.Large);
                }
                else if (_inventorySystem.TryGetSlotEntity(strippable, slot, out var slotItem))
                {
                    _popup.PopupEntity(Loc.GetString("strippable-component-alert-owner", ("user", Identity.Entity(user, EntityManager)), ("item", slotItem)), target,
                        target, PopupType.Large);
                }
            }

            _adminLogger.Add(LogType.Stripping, LogImpact.Low, $"{ToPrettyString(user):user} is trying to strip the item {ToPrettyString(item):item} from {ToPrettyString(target):target}");

            var result = await _doAfter.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished)
                return;

            if (!_inventorySystem.TryUnequip(user, strippable, slot))
                return;

            // Raise a dropped event, so that things like gas tank internals properly deactivate when stripping
            RaiseLocalEvent(item, new DroppedEvent(user), true);

            _handsSystem.PickupOrDrop(user, item, animateUser: !ev.Stealth, animate: !ev.Stealth);
            _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(user):user} has stripped the item {ToPrettyString(item):item} from {ToPrettyString(target):target}");

        }

        /// <summary>
        ///     Takes an item from a hand and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromHands(EntityUid user, EntityUid target, EntityUid item, string handName, Entity<StrippableComponent> strippable)
        {
            var hands = Comp<HandsComponent>(target);
            var userHands = Comp<HandsComponent>(user);

            bool Check()
            {
                if (!_handsSystem.TryGetHand(target, handName, out var hand, hands) || hand.HeldEntity != item)
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-item-slot-free-message",("owner", target)), user);
                    return false;
                }

                if (HasComp<HandVirtualItemComponent>(hand.HeldEntity))
                    return false;

                if (!_handsSystem.CanDropHeld(target, hand, false))
                {
                    _popup.PopupCursor(Loc.GetString("strippable-component-cannot-drop-message",("owner", target)), user);
                    return false;
                }

                return true;
            }

            var userEv = new BeforeStripEvent(strippable.Comp.HandStripDelay);
            RaiseLocalEvent(user, userEv);
            var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
            RaiseLocalEvent(target, ev);

            var doAfterArgs = new DoAfterArgs(EntityManager, user, ev.Time, new AwaitedDoAfterEvent(), null, target: target, used: item)
            {
                ExtraCheck = Check,
                Hidden = ev.Stealth,
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                BreakOnHandChange = false, // allow simultaneously removing multiple items.
                DuplicateCondition = DuplicateConditions.SameTool
            };

            if (!ev.Stealth && Check() && _handsSystem.TryGetHand(target, handName, out var handSlot, hands) && handSlot.HeldEntity != null)
            {
                _popup.PopupEntity(
                    Loc.GetString("strippable-component-alert-owner",
                    ("user", Identity.Entity(user, EntityManager)), ("item", item)),
                    strippable.Owner,
                    strippable.Owner);
            }

            _adminLogger.Add(LogType.Stripping, LogImpact.Low,
                $"{ToPrettyString(user):user} is trying to strip the item {ToPrettyString(item):item} from {ToPrettyString(target):target}");

            var result = await _doAfter.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished)
                return;

            _handsSystem.TryDrop(target, item, checkActionBlocker: false, handsComp: hands);
            _handsSystem.PickupOrDrop(user, item, animateUser: !ev.Stealth, animate: !ev.Stealth, handsComp: userHands);
            // hand update will trigger strippable update
            _adminLogger.Add(LogType.Stripping, LogImpact.Medium,
                $"{ToPrettyString(user):user} has stripped the item {ToPrettyString(item):item} from {ToPrettyString(target):target}");
        }
    }
}

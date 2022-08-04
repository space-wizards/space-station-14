using System.Threading;
using Content.Server.Cuffs.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Inventory;
using Content.Server.UserInterface;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Strip
{
    public sealed class StrippableSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        // TODO: ECS popups. Not all of these have ECS equivalents yet.

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrippableComponent, GetVerbsEvent<Verb>>(AddStripVerb);
            SubscribeLocalEvent<StrippableComponent, DidEquipEvent>(OnDidEquip);
            SubscribeLocalEvent<StrippableComponent, DidUnequipEvent>(OnDidUnequip);
            SubscribeLocalEvent<StrippableComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<StrippableComponent, CuffedStateChangeEvent>(OnCuffStateChange);

            // BUI
            SubscribeLocalEvent<StrippableComponent, StrippingInventoryButtonPressed>(OnStripInvButtonMessage);
            SubscribeLocalEvent<StrippableComponent, StrippingHandButtonPressed>(OnStripHandMessage);
            SubscribeLocalEvent<StrippableComponent, StrippingHandcuffButtonPressed>(OnStripHandcuffMessage);

            SubscribeLocalEvent<StrippableComponent, OpenStrippingCompleteEvent>(OnOpenStripComplete);
            SubscribeLocalEvent<StrippableComponent, OpenStrippingCancelledEvent>(OnOpenStripCancelled);
        }

        private void OnOpenStripCancelled(EntityUid uid, StrippableComponent component, OpenStrippingCancelledEvent args)
        {
            component.CancelTokens.Remove(args.User);
        }

        private void OnOpenStripComplete(EntityUid uid, StrippableComponent component, OpenStrippingCompleteEvent args)
        {
            component.CancelTokens.Remove(args.User);

            if (!TryComp<ActorComponent>(args.User, out var actor)) return;

            uid.GetUIOrNull(StrippingUiKey.Key)?.Open(actor.PlayerSession);
        }

        private void OnStripHandcuffMessage(EntityUid uid, StrippableComponent component, StrippingHandcuffButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user)
                return;

            if (TryComp<CuffableComponent>(component.Owner, out var cuffed))
            {
                foreach (var entity in cuffed.StoredEntities)
                {
                    if (entity != args.Handcuff) continue;
                    cuffed.TryUncuff(user, entity);
                    return;
                }
            }
        }

        private void OnStripHandMessage(EntityUid uid, StrippableComponent component, StrippingHandButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user ||
                !TryComp<HandsComponent>(user, out var userHands))
                return;

            var placingItem = userHands.ActiveHandEntity != null;

            if (TryComp<HandsComponent>(component.Owner, out var hands))
            {
                if (hands.Hands.TryGetValue(args.Hand, out var hand) && !hand.IsEmpty)
                    placingItem = false;

                if (placingItem)
                    PlaceActiveHandItemInHands(user, args.Hand, component);
                else
                    TakeItemFromHands(user, args.Hand, component);
            }
        }

        private void OnStripInvButtonMessage(EntityUid uid, StrippableComponent component, StrippingInventoryButtonPressed args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} user ||
                !TryComp<HandsComponent>(user, out var userHands))
                return;

            var placingItem = userHands.ActiveHandEntity != null;

            if (TryComp<InventoryComponent>(component.Owner, out var inventory))
            {
                if (_inventorySystem.TryGetSlotEntity(component.Owner, args.Slot, out _, inventory))
                    placingItem = false;

                if (placingItem)
                    PlaceActiveHandItemInInventory(user, args.Slot, component);
                else
                    TakeItemFromInventory(user, args.Slot, component);
            }
        }

        public void StartOpeningStripper(EntityUid user, StrippableComponent component)
        {
            if (component.CancelTokens.ContainsKey(user)) return;

            if (TryComp<ActorComponent>(user, out var actor))
            {
                if (component.Owner.GetUIOrNull(StrippingUiKey.Key)?.SessionHasOpen(actor.PlayerSession) == true)
                    return;
            }

            var token = new CancellationTokenSource();

            var doAfterArgs = new DoAfterEventArgs(user, component.OpenDelay, token.Token, component.Owner)
            {
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
                TargetCancelledEvent = new OpenStrippingCancelledEvent(user),
                TargetFinishedEvent = new OpenStrippingCompleteEvent(user),
            };

            component.CancelTokens[user] = token;
            _doAfterSystem.DoAfter(doAfterArgs);
        }

        private void OnCompInit(EntityUid uid, StrippableComponent component, ComponentInit args)
        {
            EnsureComp<ServerInventoryComponent>(uid);
            SendUpdate(uid, component);
        }

        private void OnCuffStateChange(EntityUid uid, StrippableComponent component, ref CuffedStateChangeEvent args)
        {
            UpdateState(uid, component);
        }

        private void OnDidUnequip(EntityUid uid, StrippableComponent component, DidUnequipEvent args)
        {
            SendUpdate(uid, component);
        }

        private void OnDidEquip(EntityUid uid, StrippableComponent component, DidEquipEvent args)
        {
            SendUpdate(uid, component);
        }

        public void SendUpdate(EntityUid uid, StrippableComponent? strippableComponent = null)
        {
            var bui = uid.GetUIOrNull(StrippingUiKey.Key);

            if (!Resolve(uid, ref strippableComponent, false) || bui == null)
            {
                return;
            }

            var cuffs = new Dictionary<EntityUid, string>();
            var inventory = new Dictionary<(string ID, string Name), string>();
            var hands = new Dictionary<string, string>();

            if (TryComp(uid, out CuffableComponent? cuffed))
            {
                foreach (var entity in cuffed.StoredEntities)
                {
                    var name = Name(entity);
                    cuffs.Add(entity, name);
                }
            }

            if (_inventorySystem.TryGetSlots(uid, out var slots))
            {
                foreach (var slot in slots)
                {
                    var name = "None";

                    if (_inventorySystem.TryGetSlotEntity(uid, slot.Name, out var item))
                    {
                        if (!slot.StripHidden)
                        {
                            name = Name(item.Value);
                        }

                        else
                        {
                            name = Loc.GetString("strippable-bound-user-interface-stripping-menu-obfuscate");
                        }
                    }

                    inventory[(slot.Name, slot.DisplayName)] = name;
                }
            }

            if (TryComp(uid, out HandsComponent? handsComp))
            {
                foreach (var hand in handsComp.Hands.Values)
                {
                    if (hand.HeldEntity == null || HasComp<HandVirtualItemComponent>(hand.HeldEntity))
                    {
                        hands[hand.Name] = "None";
                        continue;
                    }

                    hands[hand.Name] = Name(hand.HeldEntity.Value);
                }
            }

            bui.SetState(new StrippingBoundUserInterfaceState(inventory, hands, cuffs));
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
                IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png",
                Act = () => StartOpeningStripper(args.User, component),
            };
            args.Verbs.Add(verb);
        }

        private void UpdateState(EntityUid uid, StrippableComponent component)
        {
            SendUpdate(uid, component);
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

            var ev = new BeforeStripEvent(slotDef.StripTime);
            RaiseLocalEvent(user, ev);
            var finalStripTime = ev.Time + ev.Additive;

            var doAfterArgs = new DoAfterEventArgs(user, finalStripTime, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (!ev.Stealth && Check() && userHands.ActiveHandEntity != null)
            {
                var message = Loc.GetString("strippable-component-alert-owner-insert",
                    ("user", Identity.Entity(user, EntityManager)), ("item", userHands.ActiveHandEntity));
                _popupSystem.PopupEntity(message, component.Owner, Filter.Entities(component.Owner), PopupType.Large);
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHand?.HeldEntity is { } held
                && _handsSystem.TryDrop(user, userHands.ActiveHand, handsComp: userHands))
            {
                _inventorySystem.TryEquip(user, component.Owner, held, slot);
            }

            UpdateState(component.Owner, component);
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

            var doAfterArgs = new DoAfterEventArgs(user, component.HandStripDelay, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (Check() && userHands.Hands.TryGetValue(handName, out var handSlot))
            {
                if (handSlot.HeldEntity != null)
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner-insert", ("user", Identity.Entity(user, EntityManager)), ("item", handSlot.HeldEntity)), component.Owner,
                        Filter.Entities(component.Owner), PopupType.Large);
                }
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHandEntity is not { } held)
                return;

            _handsSystem.TryDrop(user, checkActionBlocker: false, handsComp: userHands);
            _handsSystem.TryPickup(component.Owner, held, handName, checkActionBlocker: false, animateUser: true, handsComp: hands);
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

                if (!_inventorySystem.CanUnequip(user, component.Owner, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-unequip-message", ("owner", component.Owner)));
                    return false;
                }

                return true;
            }

            if (!_inventorySystem.TryGetSlot(component.Owner, slot, out var slotDef))
            {
                Logger.Error($"{ToPrettyString(user)} attempted to take an item from a non-existent inventory slot ({slot}) on {ToPrettyString(component.Owner)}");
                return;
            }

            var ev = new BeforeStripEvent(slotDef.StripTime);
            RaiseLocalEvent(user, ev);
            var finalStripTime = ev.Time + ev.Additive;

            var doAfterArgs = new DoAfterEventArgs(user, finalStripTime, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            if (!ev.Stealth && Check())
            {
                if (slotDef.StripHidden)
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner-hidden", ("slot", slot)), component.Owner,
                        Filter.Entities(component.Owner), PopupType.Large);
                }
                else if (_inventorySystem.TryGetSlotEntity(component.Owner, slot, out var slotItem))
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner", ("user", Identity.Entity(user, EntityManager)), ("item", slotItem)), component.Owner,
                        Filter.Entities(component.Owner), PopupType.Large);
                }
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (_inventorySystem.TryGetSlotEntity(component.Owner, slot, out var item) && _inventorySystem.TryUnequip(user, component.Owner, slot))
            {
                // Raise a dropped event, so that things like gas tank internals properly deactivate when stripping
                RaiseLocalEvent(item.Value, new DroppedEvent(user), true);

                _handsSystem.PickupOrDrop(user, item.Value);
            }

            UpdateState(component.Owner, component);
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

            var ev = new BeforeStripEvent(component.HandStripDelay);
            RaiseLocalEvent(user, ev);
            var finalStripTime = ev.Time + ev.Additive;

            var doAfterArgs = new DoAfterEventArgs(user, finalStripTime, CancellationToken.None, component.Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            if (Check() && hands.Hands.TryGetValue(handName, out var handSlot))
            {
                if (handSlot.HeldEntity != null)
                {
                    _popupSystem.PopupEntity(Loc.GetString("strippable-component-alert-owner", ("user", Identity.Entity(user, EntityManager)), ("item", handSlot.HeldEntity)), component.Owner, Filter.Entities(component.Owner));
                }
            }

            var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (!hands.Hands.TryGetValue(handName, out var hand) || hand.HeldEntity is not { } held)
                return;

            _handsSystem.TryDrop(component.Owner, hand, checkActionBlocker: false, handsComp: hands);
            _handsSystem.PickupOrDrop(user, held, handsComp: userHands);
            // hand update will trigger strippable update
        }

        private sealed class OpenStrippingCompleteEvent
        {
            public readonly EntityUid User;

            public OpenStrippingCompleteEvent(EntityUid user)
            {
                User = user;
            }
        }

        private sealed class OpenStrippingCancelledEvent
        {
            public readonly EntityUid User;

            public OpenStrippingCancelledEvent(EntityUid user)
            {
                User = user;
            }
        }
    }
}

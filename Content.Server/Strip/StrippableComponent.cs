using System.Threading;
using Content.Server.Cuffs.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Inventory;
using Content.Server.UserInterface;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    [Friend(typeof(StrippableSystem))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        private StrippableSystem _strippableSystem = default!;

        public const float StripDelay = 2f;

        // TODO: This component needs localization.

        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(StrippingUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += HandleUserInterfaceMessage;
            }

            _strippableSystem = EntitySystem.Get<StrippableSystem>();
            Owner.EnsureComponentWarn<ServerInventoryComponent>();
            if(_entities.TryGetComponent<CuffableComponent>(Owner, out var cuffed))
                cuffed.OnCuffedStateChanged += UpdateState;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if(_entities.TryGetComponent<CuffableComponent>(Owner, out var cuffed))
                cuffed.OnCuffedStateChanged -= UpdateState;
        }

        private void UpdateState()
        {
            _strippableSystem.SendUpdate(Owner, this);
        }

        public override bool Drop(DragDropEvent args)
        {
            if (!_entities.TryGetComponent(args.User, out ActorComponent? actor)) return false;

            OpenUserInterface(actor.PlayerSession);
            return true;
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        /// <summary>
        ///     Places item in user's active hand to an inventory slot.
        /// </summary>
        private async void PlaceActiveHandItemInInventory(EntityUid user, string slot)
        {
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var invSystem = _sysMan.GetEntitySystem<InventorySystem>();
            var handSys = _sysMan.GetEntitySystem<SharedHandsSystem>();

            bool Check()
            {
                if (userHands.ActiveHand?.HeldEntity is not EntityUid held)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!handSys.CanDropHeld(user, userHands.ActiveHand))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop"));
                    return false;
                }

                if (!invSystem.HasSlot(Owner, slot))
                    return false;

                if (invSystem.TryGetSlotEntity(Owner, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-occupied",("owner", Owner)));
                    return false;
                }

                if (!invSystem.CanEquip(user, Owner, held, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-equip-message",("owner", Owner)));
                    return false;
                }

                return true;
            }

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, StripDelay, CancellationToken.None, Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHand?.HeldEntity is EntityUid held
                && handSys.TryDrop(user, userHands.ActiveHand, handsComp: userHands))
            {
                invSystem.TryEquip(user, Owner, held, slot);
            }

            UpdateState();
        }

        /// <summary>
        ///     Places item in user's active hand in one of the entity's hands.
        /// </summary>
        private async void PlaceActiveHandItemInHands(EntityUid user, string handName)
        {
            var hands = _entities.GetComponent<HandsComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var sys = _sysMan.GetEntitySystem<SharedHandsSystem>();

            bool Check()
            {
                if (userHands.ActiveHandEntity == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!sys.CanDropHeld(user, userHands.ActiveHand!))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop"));
                    return false;
                }

                if (!hands.Hands.TryGetValue(handName, out var hand)
                    || !sys.CanPickupToHand(Owner, userHands.ActiveHandEntity.Value, hand, checkActionBlocker: false, hands))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-put-message",("owner", Owner)));
                    return false;
                }

                return true;
            }

            var doAfterSystem = _sysMan.GetEntitySystem<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, StripDelay, CancellationToken.None, Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (userHands.ActiveHandEntity is not EntityUid held)
                return;

            sys.TryDrop(user, checkActionBlocker: false, handsComp: userHands);
            sys.TryPickup(Owner, held, handName, checkActionBlocker: false, animateUser: true, handsComp: hands);
            // hand update will trigger strippable update
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(EntityUid user, string slot)
        {
            var inventory = _entities.GetComponent<InventoryComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var invSystem = _sysMan.GetEntitySystem<InventorySystem>();

            bool Check()
            {
                if (!invSystem.HasSlot(Owner, slot))
                    return false;

                if (!invSystem.TryGetSlotEntity(Owner, slot, out var item))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-free-message",("owner", Owner)));
                    return false;
                }

                if (!invSystem.CanUnequip(user, Owner, slot, out _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-unequip-message",("owner", Owner)));
                    return false;
                }

                return true;
            }

            var doAfterSystem = _sysMan.GetEntitySystem<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, StripDelay, CancellationToken.None, Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (invSystem.TryGetSlotEntity(Owner, slot, out var item) && invSystem.TryUnequip(user, Owner, slot))
            {
                // Raise a dropped event, so that things like gas tank internals properly deactivate when stripping
                _entities.EventBus.RaiseLocalEvent(item.Value, new DroppedEvent(user));

                _sysMan.GetEntitySystem<SharedHandsSystem>().PickupOrDrop(user, item.Value);
            }

            UpdateState();
        }

        /// <summary>
        ///     Takes an item from a hand and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromHands(EntityUid user, string handName)
        {
            var hands = _entities.GetComponent<HandsComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var handSys = _sysMan.GetEntitySystem<SharedHandsSystem>();

            bool Check()
            {
                if (!hands.Hands.TryGetValue(handName, out var hand) || hand.HeldEntity == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-free-message",("owner", Owner)));
                    return false;
                }

                if (_entities.HasComponent<HandVirtualItemComponent>(hand.HeldEntity))
                    return false;

                if (!handSys.CanDropHeld(Owner, hand, false))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop-message",("owner", Owner)));
                    return false;
                }

                return true;
            }

            var doAfterSystem = _sysMan.GetEntitySystem<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, StripDelay, CancellationToken.None, Owner)
            {
                ExtraCheck = Check,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            };

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (!hands.Hands.TryGetValue(handName, out var hand) || hand.HeldEntity is not EntityUid held)
                return;

            handSys.TryDrop(Owner, hand, checkActionBlocker: false, handsComp: hands);
            handSys.PickupOrDrop(user, held, handsComp: userHands);
            // hand update will trigger strippable update
        }

        private void HandleUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} user ||
                !_entities.TryGetComponent(user, out HandsComponent? userHands))
                return;

            var placingItem = userHands.ActiveHandEntity != null;

            switch (obj.Message)
            {
                case StrippingInventoryButtonPressed inventoryMessage:
                    if (_entities.TryGetComponent<InventoryComponent?>(Owner, out var inventory))
                    {
                        if (_sysMan.GetEntitySystem<InventorySystem>().TryGetSlotEntity(Owner, inventoryMessage.Slot, out _, inventory))
                            placingItem = false;

                        if (placingItem)
                            PlaceActiveHandItemInInventory(user, inventoryMessage.Slot);
                        else
                            TakeItemFromInventory(user, inventoryMessage.Slot);
                    }
                    break;

                case StrippingHandButtonPressed handMessage:

                    if (_entities.TryGetComponent<HandsComponent?>(Owner, out var hands))
                    {
                        if (hands.Hands.TryGetValue(handMessage.Hand, out var hand) && !hand.IsEmpty)
                            placingItem = false;

                        if (placingItem)
                            PlaceActiveHandItemInHands(user, handMessage.Hand);
                        else
                            TakeItemFromHands(user, handMessage.Hand);
                    }
                    break;

                case StrippingHandcuffButtonPressed handcuffMessage:

                    if (_entities.TryGetComponent<CuffableComponent?>(Owner, out var cuffed))
                    {
                        foreach (var entity in cuffed.StoredEntities)
                        {
                            if (entity == handcuffMessage.Handcuff)
                            {
                                cuffed.TryUncuff(user, entity);
                                return;
                            }
                        }
                    }
                    break;
            }
        }
    }
}

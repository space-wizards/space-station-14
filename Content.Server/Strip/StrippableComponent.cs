using System.Collections.Generic;
using System.Threading;
using Content.Server.Cuffs.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Inventory;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    [Friend(typeof(StrippableSystem))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
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
            var cuffed = Owner.EnsureComponentWarn<CuffableComponent>();
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
            var item = userHands.GetActiveHandItem;
            var invSystem = EntitySystem.Get<InventorySystem>();

            bool Check()
            {
                if (item == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!userHands.CanDrop(userHands.ActiveHand!))
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

                if (!invSystem.CanEquip(user, Owner, item.Owner, slot, out _))
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

            userHands.Drop(item!.Owner, false);
            invSystem.TryEquip(user, Owner, item.Owner, slot);

            UpdateState();
        }

        /// <summary>
        ///     Places item in user's active hand in one of the entity's hands.
        /// </summary>
        private async void PlaceActiveHandItemInHands(EntityUid user, string hand)
        {
            var hands = _entities.GetComponent<HandsComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var item = userHands.GetActiveHandItem;

            bool Check()
            {
                if (item == null)
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-not-holding-anything"));
                    return false;
                }

                if (!userHands.CanDrop(userHands.ActiveHand!))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop"));
                    return false;
                }

                if (!hands.HasHand(hand))
                {
                    return false;
                }

                if (hands.TryGetItem(hand, out var _))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-occupied-message", ("owner", Owner)));
                    return false;
                }

                if (!hands.CanPickupEntity(hand, item.Owner, checkActionBlocker: false))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-put-message",("owner", Owner)));
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

            userHands.Drop(hand);
            hands.TryPickupEntity(hand, item!.Owner, checkActionBlocker: false, animateUser: true);
            // hand update will trigger strippable update
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(EntityUid user, string slot)
        {
            var inventory = _entities.GetComponent<InventoryComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);
            var invSystem = EntitySystem.Get<InventorySystem>();

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

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

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
                userHands.PutInHandOrDrop(item.Value);
            }

            UpdateState();
        }

        /// <summary>
        ///     Takes an item from a hand and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromHands(EntityUid user, string hand)
        {
            var hands = _entities.GetComponent<HandsComponent>(Owner);
            var userHands = _entities.GetComponent<HandsComponent>(user);

            bool Check()
            {
                if (!hands.HasHand(hand))
                    return false;

                if (!hands.TryGetItem(hand, out var heldItem))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-item-slot-free-message",("owner", Owner)));
                    return false;
                }

                if (_entities.HasComponent<HandVirtualItemComponent>(heldItem.Owner))
                    return false;

                if (!hands.CanDrop(hand, false))
                {
                    user.PopupMessageCursor(Loc.GetString("strippable-component-cannot-drop-message",("owner", Owner)));
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
            };

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            if (!hands.TryGetHeldEntity(hand, out var entity))
                return;

            hands.Drop(hand, false);
            userHands.PutInHandOrDrop(entity.Value);
            // hand update will trigger strippable update
        }

        private void HandleUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} user ||
                !_entities.TryGetComponent(user, out HandsComponent? userHands))
                return;

            var placingItem = userHands.GetActiveHandItem != null;

            switch (obj.Message)
            {
                case StrippingInventoryButtonPressed inventoryMessage:
                    if (_entities.TryGetComponent<InventoryComponent?>(Owner, out var inventory))
                    {
                        if (EntitySystem.Get<InventorySystem>().TryGetSlotEntity(Owner, inventoryMessage.Slot, out _, inventory))
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
                        if (hands.TryGetItem(handMessage.Hand, out _))
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

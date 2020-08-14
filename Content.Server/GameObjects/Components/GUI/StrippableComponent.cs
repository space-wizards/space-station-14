using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    public sealed class StrippableComponent : SharedStrippableComponent, IDragDrop
    {
        [Dependency] private IServerNotifyManager _notifyManager = default!;

        public const float StripDelay = 2f;

        [ViewVariables]
        private BoundUserInterface _userInterface;

        private InventoryComponent _inventoryComponent;
        private HandsComponent _handsComponent;

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(StrippingUiKey.Key);
            _userInterface.OnReceiveMessage += HandleUserInterfaceMessage;

            _inventoryComponent = Owner.GetComponent<InventoryComponent>();
            _handsComponent = Owner.GetComponent<HandsComponent>();

            _inventoryComponent.OnItemChanged += UpdateSubscribed;

            // Initial update.
            UpdateSubscribed();
        }

        private void UpdateSubscribed()
        {
            var inventory = GetInventorySlots();
            var hands = GetHandSlots();

            _userInterface.SetState(new StrippingBoundUserInterfaceState(inventory, hands));
        }

        public bool CanDragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.User.HasComponent<HandsComponent>()
                   && eventArgs.Target != eventArgs.Dropped && eventArgs.Target == eventArgs.User;
        }

        public bool DragDrop(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor)) return false;

            OpenUserInterface(actor.playerSession);
            return true;
        }

        private Dictionary<EquipmentSlotDefines.Slots, string> GetInventorySlots()
        {
            var dictionary = new Dictionary<EquipmentSlotDefines.Slots, string>();

            foreach (var (slot, container) in _inventoryComponent.SlotContainers)
            {
                dictionary[slot] = container.ContainedEntity?.Name ?? "None";
            }

            return dictionary;
        }

        private Dictionary<string, string> GetHandSlots()
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var hand in _handsComponent.Hands)
            {
                dictionary[hand.Name] = hand.Container.ContainedEntity?.Name ?? "None";
            }

            return dictionary;
        }

        private void OpenUserInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        /// <summary>
        ///     Places item in user's active hand to an inventory slot.
        /// </summary>
        private async void PlaceActiveHandItemInInventory(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            var inventory = Owner.GetComponent<InventoryComponent>();
            var userHands = user.GetComponent<HandsComponent>();
            var item = userHands.GetActiveHand;

            bool Check()
            {
                if (!ActionBlockerSystem.CanInteract(user))
                    return false;

                if (item == null)
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("You aren't holding anything!"));
                    return false;
                }

                if (!userHands.CanDrop(userHands.ActiveHand!))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("You can't drop that!"));
                    return false;
                }

                if (!inventory.HasSlot(slot))
                    return false;

                if (inventory.TryGetSlotItem(slot, out ItemComponent _))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} already {0:have} something there!", Owner));
                    return false;
                }

                if (!inventory.CanEquip(slot, item))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} cannot equip that there!", Owner));
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

            var result = await doAfterSystem.DoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            userHands.Drop(item!.Owner, false);
            inventory.Equip(slot, item!.Owner);

            UpdateSubscribed();
        }

        /// <summary>
        ///     Places item in user's active hand in one of the entity's hands.
        /// </summary>
        private async void PlaceActiveHandItemInHands(IEntity user, string hand)
        {
            var hands = Owner.GetComponent<HandsComponent>();
            var userHands = user.GetComponent<HandsComponent>();
            var item = userHands.GetActiveHand;

            bool Check()
            {
                if (!ActionBlockerSystem.CanInteract(user))
                    return false;

                if (item == null)
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("You aren't holding anything!"));
                    return false;
                }

                if (!userHands.CanDrop(userHands.ActiveHand!))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("You can't drop that!"));
                    return false;
                }

                if (!hands.HasHand(hand))
                    return false;

                if (hands.TryGetItem(hand, out var _))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} already {0:have} something there!", Owner));
                    return false;
                }

                if (!hands.CanPutInHand(item, hand))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} cannot put that there!", Owner));
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

            var result = await doAfterSystem.DoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            userHands.Drop(hand, false);
            hands.PutInHand(item, hand, false);
            UpdateSubscribed();
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(IEntity user, EquipmentSlotDefines.Slots slot)
        {
            var inventory = Owner.GetComponent<InventoryComponent>();
            var userHands = user.GetComponent<HandsComponent>();

            bool Check()
            {
                if (!ActionBlockerSystem.CanInteract(user))
                    return false;

                if (!inventory.HasSlot(slot))
                    return false;

                if (!inventory.TryGetSlotItem(slot, out ItemComponent itemToTake))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} {0:have} nothing there!", Owner));
                    return false;
                }

                if (!inventory.CanUnequip(slot))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} cannot unequip that!", Owner));
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

            var result = await doAfterSystem.DoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            var item = inventory.GetSlotItem(slot);
            inventory.Unequip(slot);
            userHands.PutInHandOrDrop(item);
            UpdateSubscribed();
        }

        /// <summary>
        ///     Takes an item from a hand and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromHands(IEntity user, string hand)
        {
            var hands = Owner.GetComponent<HandsComponent>();
            var userHands = user.GetComponent<HandsComponent>();

            bool Check()
            {
                if (!ActionBlockerSystem.CanInteract(user))
                    return false;

                if (!hands.HasHand(hand))
                    return false;

                if (!hands.TryGetItem(hand, out var heldItem))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} {0:have} nothing there!", Owner));
                    return false;
                }

                if (!hands.CanDrop(hand))
                {
                    _notifyManager.PopupMessageCursor(user, Loc.GetString("{0:They} cannot drop that!", Owner));
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

            var result = await doAfterSystem.DoAfter(doAfterArgs);
            if (result != DoAfterStatus.Finished) return;

            var item = hands.GetItem(hand);
            hands.Drop(hand, false);
            userHands.PutInHandOrDrop(item);
            UpdateSubscribed();
        }

        private void HandleUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            var user = obj.Session.AttachedEntity;
            if (user == null || !(user.TryGetComponent(out HandsComponent userHands))) return;

            var placingItem = userHands.GetActiveHand != null;

            switch (obj.Message)
            {
                case StrippingInventoryButtonPressed inventoryMessage:
                    var inventory = Owner.GetComponent<InventoryComponent>();

                    if (inventory.TryGetSlotItem(inventoryMessage.Slot, out ItemComponent _))
                        placingItem = false;

                    if(placingItem)
                        PlaceActiveHandItemInInventory(user, inventoryMessage.Slot);
                    else
                        TakeItemFromInventory(user, inventoryMessage.Slot);
                    break;
                case StrippingHandButtonPressed handMessage:
                    var hands = Owner.GetComponent<HandsComponent>();

                    if (hands.TryGetItem(handMessage.Hand, out _))
                        placingItem = false;

                    if(placingItem)
                        PlaceActiveHandItemInHands(user, handMessage.Hand);
                    else
                        TakeItemFromHands(user, handMessage.Hand);
                    break;
                default:
                    break;
            }
        }
    }
}

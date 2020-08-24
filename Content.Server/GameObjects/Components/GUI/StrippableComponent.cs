#nullable enable
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.GUI;
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
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    public sealed class StrippableComponent : SharedStrippableComponent, IDragDrop
    {
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public const float StripDelay = 2f;

        [ViewVariables]
        private BoundUserInterface? UserInterface =>
            Owner.TryGetComponent(out ServerUserInterfaceComponent? ui) &&
            ui.TryGetBoundUserInterface(StrippingUiKey.Key, out var boundUi)
                ? boundUi
                : null;

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += HandleUserInterfaceMessage;
            }

            if (Owner.TryGetComponent(out InventoryComponent? inventory))
            {
                inventory.OnItemChanged += UpdateSubscribed;
            }

            if (Owner.TryGetComponent(out HandsComponent? hands))
            {
                hands.OnItemChanged += UpdateSubscribed;
            }

            // Initial update.
            UpdateSubscribed();
        }

        private void UpdateSubscribed()
        {
            if (UserInterface == null)
            {
                return;
            }

            var inventory = GetInventorySlots();
            var hands = GetHandSlots();

            UserInterface.SetState(new StrippingBoundUserInterfaceState(inventory, hands));
        }

        public bool CanDragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.User.HasComponent<HandsComponent>()
                   && eventArgs.Target != eventArgs.Dropped && eventArgs.Target == eventArgs.User;
        }

        public bool DragDrop(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor)) return false;

            OpenUserInterface(actor.playerSession);
            return true;
        }

        private Dictionary<Slots, string> GetInventorySlots()
        {
            var dictionary = new Dictionary<Slots, string>();

            if (!Owner.TryGetComponent(out InventoryComponent? inventory))
            {
                return dictionary;
            }

            foreach (var slot in inventory.Slots)
            {
                dictionary[slot] = inventory.GetSlotItem(slot)?.Owner.Name ?? "None";
            }

            return dictionary;
        }

        private Dictionary<string, string> GetHandSlots()
        {
            var dictionary = new Dictionary<string, string>();

            if (!Owner.TryGetComponent(out HandsComponent? hands))
            {
                return dictionary;
            }

            foreach (var hand in hands.Hands)
            {
                dictionary[hand] = hands.GetItem(hand)?.Owner.Name ?? "None";
            }

            return dictionary;
        }

        private void OpenUserInterface(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        /// <summary>
        ///     Places item in user's active hand to an inventory slot.
        /// </summary>
        private async void PlaceActiveHandItemInInventory(IEntity user, Slots slot)
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

                if (!inventory.CanEquip(slot, item, false))
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
            inventory.Equip(slot, item!.Owner, false);

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

                if (!hands.CanPutInHand(item, hand, false))
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
            hands.PutInHand(item!, hand, false, false);
            UpdateSubscribed();
        }

        /// <summary>
        ///     Takes an item from the inventory and places it in the user's active hand.
        /// </summary>
        private async void TakeItemFromInventory(IEntity user, Slots slot)
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

                if (!inventory.CanUnequip(slot, false))
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
            inventory.Unequip(slot, false);
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

                if (!hands.CanDrop(hand, false))
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
            userHands.PutInHandOrDrop(item!);
            UpdateSubscribed();
        }

        private void HandleUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            var user = obj.Session.AttachedEntity;
            if (user == null || !(user.TryGetComponent(out HandsComponent? userHands))) return;

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
            }
        }
    }
}

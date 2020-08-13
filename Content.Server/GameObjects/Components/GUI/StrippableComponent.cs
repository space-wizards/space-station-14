using System.Collections.Generic;
using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    public sealed class StrippableComponent : SharedStrippableComponent, IDragDrop
    {
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

        private void HandleUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj)
            {
                default:
                    break;
            }
        }
    }
}

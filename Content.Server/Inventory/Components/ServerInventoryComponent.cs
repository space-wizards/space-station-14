using System;
using System.Collections.Generic;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Storage.Components;
using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Server.Inventory.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedInventoryComponent))]
    public class ServerInventoryComponent : SharedInventoryComponent
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        /// <summary>
        /// Message that tells us to equip or unequip items from the inventory slots
        /// </summary>
        /// <param name="msg"></param>
        private async void HandleInventoryMessage(ClientInventoryMessage msg)
        {
            switch (msg.Updatetype)
            {
                case ClientInventoryUpdate.Equip:
                {
                    var hands = _entities.GetComponent<HandsComponent>(Owner);
                    var activeHand = hands.ActiveHand;
                    var activeItem = hands.GetActiveHand;
                    if (activeHand != null && activeItem != null && _entities.TryGetComponent(activeItem.Owner, out ItemComponent? item))
                    {
                        hands.TryDropNoInteraction();
                        if (!Equip(msg.Inventoryslot, item, true, out var reason))
                        {
                            hands.PutInHand(item);
                            Owner.PopupMessageCursor(reason);
                        }
                    }

                    break;
                }
                case ClientInventoryUpdate.Use:
                {
                    var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                    var hands = _entities.GetComponent<HandsComponent>(Owner);
                    var activeHand = hands.GetActiveHand;
                    var itemContainedInSlot = GetSlotItem(msg.Inventoryslot);
                    if (itemContainedInSlot != null)
                    {
                        if (activeHand != null)
                        {
                            await interactionSystem.InteractUsing(Owner, activeHand.Owner, itemContainedInSlot.Owner,
                                new EntityCoordinates());
                        }
                        else if (Unequip(msg.Inventoryslot))
                        {
                            hands.PutInHand(itemContainedInSlot);
                        }
                    }

                    break;
                }
                case ClientInventoryUpdate.Hover:
                {
                    var hands = _entities.GetComponent<HandsComponent>(Owner);
                    var activeHand = hands.GetActiveHand;
                    if (activeHand != null && GetSlotItem(msg.Inventoryslot) == null)
                    {
                        var canEquip = CanEquip(msg.Inventoryslot, activeHand, true, out var reason);
                        _hoverEntity =
                            new KeyValuePair<Slots, (EntityUid entity, bool fits)>(msg.Inventoryslot,
                                (Uid: activeHand.Owner, canEquip));

                        Dirty();
                    }

                    break;
                }
            }
        }

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel,
            ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClientInventoryMessage msg:
                    var playerentity = session.AttachedEntity;

                    if (playerentity == Owner)
                        HandleInventoryMessage(msg);
                    break;

                case OpenSlotStorageUIMessage msg:
                    if (!HasSlot(msg.Slot)) // client input sanitization
                        return;
                    var item = GetSlotItem(msg.Slot);
                    if (item != null && _entities.TryGetComponent(item.Owner, out ServerStorageComponent? storage))
                        storage.OpenStorageUI(Owner);
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            var list = new List<KeyValuePair<Slots, EntityUid>>();
            foreach (var (slot, container) in _slotContainers)
            {
                if (container is {ContainedEntity: { }})
                {
                    list.Add(new KeyValuePair<Slots, EntityUid>(slot, container.ContainedEntity.Value));
                }
            }

            var hover = _hoverEntity;
            _hoverEntity = null;

            return new InventoryComponentState(list, hover);
        }
    }
}

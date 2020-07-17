using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using static Content.Shared.GameObjects.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    public class InventoryComponent : SharedInventoryComponent, IExAct, IEffectBlocker
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IServerNotifyManager _serverNotifyManager;
#pragma warning restore 649

        [ViewVariables]
        private readonly Dictionary<Slots, ContainerSlot> SlotContainers = new Dictionary<Slots, ContainerSlot>();

        public override void Initialize()
        {
            base.Initialize();

            foreach (var slotName in InventoryInstance.SlotMasks)
            {
                if (slotName != Slots.NONE)
                {
                    AddSlot(slotName);
                }
            }
        }

        bool IEffectBlocker.CanSlip()
        {
            if(Owner.TryGetComponent(out InventoryComponent inventoryComponent) &&
                inventoryComponent.TryGetSlotItem(EquipmentSlotDefines.Slots.SHOES, out ItemComponent shoes)
            )
            {
                return EffectBlockerSystem.CanSlip(shoes.Owner);
            }

            return true;
        }

        public override void OnRemove()
        {
            var slots = SlotContainers.Keys.ToList();
            foreach (var slot in slots)
            {
                RemoveSlot(slot);
            }

            base.OnRemove();
        }

        /// <summary>
        /// Helper to get container name for specified slot on this component
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        private string GetSlotString(Slots slot)
        {
            return Name + "_" + Enum.GetName(typeof(Slots), slot);
        }

        /// <summary>
        ///     Gets the clothing equipped to the specified slot.
        /// </summary>
        /// <param name="slot">The slot to get the item for.</param>
        /// <returns>Null if the slot is empty, otherwise the item.</returns>
        public ItemComponent GetSlotItem(Slots slot)
        {
            return GetSlotItem<ItemComponent>(slot);
        }
        public T GetSlotItem<T>(Slots slot) where T : ItemComponent
        {
            var containedEntity = SlotContainers[slot].ContainedEntity;
            if (containedEntity?.Deleted == true)
            {
                SlotContainers[slot] = null;
                containedEntity = null;
                Dirty();
            }
            return containedEntity?.GetComponent<T>();
        }

        public bool TryGetSlotItem<T>(Slots slot, out T itemComponent) where T : ItemComponent
        {
            itemComponent = GetSlotItem<T>(slot);
            return itemComponent != null;
        }

        /// <summary>
        ///     Equips slothing to the specified slot.
        /// </summary>
        /// <remarks>
        ///     This will fail if there is already an item in the specified slot.
        /// </remarks>
        /// <param name="slot">The slot to put the item in.</param>
        /// <param name="item">The item to insert into the slot.</param>
        /// <param name="reason">The translated reason why the item cannot be equiped, if this function returns false. Can be null.</param>
        /// <returns>True if the item was successfully inserted, false otherwise.</returns>
        public bool Equip(Slots slot, ItemComponent item, out string reason)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item),
                    "Clothing must be passed here. To remove some clothing from a slot, use Unequip()");
            }

            if (!CanEquip(slot, item, out reason))
            {
                return false;
            }

            var inventorySlot = SlotContainers[slot];
            if (!inventorySlot.Insert(item.Owner))
            {
                return false;
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedInteraction(Owner, item.Owner, slot);

            Dirty();

            return true;
        }

        public bool Equip(Slots slot, ItemComponent item) => Equip(slot, item, out var _);

        public bool Equip(Slots slot, IEntity entity) => Equip(slot, entity.GetComponent<ItemComponent>());

        /// <summary>
        ///     Checks whether an item can be put in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <param name="item">The item to check for.</param>
        /// <param name="reason">The translated reason why the item cannot be equiped, if this function returns false. Can be null.</param>
        /// <returns>True if the item can be inserted into the specified slot.</returns>
        public bool CanEquip(Slots slot, ItemComponent item, out string reason)
        {
            var pass = false;
            reason = null;

            if (!ActionBlockerSystem.CanEquip(Owner))
                return false;

            if (item is ClothingComponent clothing)
            {
                if (clothing.SlotFlags != SlotFlags.PREVENTEQUIP && (clothing.SlotFlags & SlotMasks[slot]) != 0)
                {
                    pass = true;
                }
                else
                {
                    reason = Loc.GetString("This doesn't fit.");
                }
            }

            if (Owner.TryGetComponent(out IInventoryController controller))
            {
                pass = controller.CanEquip(slot, item.Owner, pass, out var controllerReason);
                reason = controllerReason ?? reason;
            }

            if (!pass && reason == null)
            {
                reason = Loc.GetString("You can't equip this!");
            }

            return pass && SlotContainers[slot].CanInsert(item.Owner);
        }

        public bool CanEquip(Slots slot, ItemComponent item) => CanEquip(slot, item, out var _);

        public bool CanEquip(Slots slot, IEntity entity) => CanEquip(slot, entity.GetComponent<ItemComponent>());

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        public bool Unequip(Slots slot)
        {
            if (!CanUnequip(slot))
            {
                return false;
            }

            var inventorySlot = SlotContainers[slot];
            var item = inventorySlot.ContainedEntity.GetComponent<ItemComponent>();
            if (!inventorySlot.Remove(inventorySlot.ContainedEntity))
            {
                return false;
            }

            // TODO: The item should be dropped to the container our owner is in, if any.
            var itemTransform = item.Owner.GetComponent<ITransformComponent>();
            itemTransform.GridPosition = Owner.GetComponent<ITransformComponent>().GridPosition;

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedInteraction(Owner, item.Owner, slot);

            Dirty();

            return true;
        }

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanUnequip(Slots slot)
        {
            if (!ActionBlockerSystem.CanUnequip(Owner))
                return false;

            var InventorySlot = SlotContainers[slot];
            return InventorySlot.ContainedEntity != null && InventorySlot.CanRemove(InventorySlot.ContainedEntity);
        }

        /// <summary>
        ///     Adds a new slot to this inventory component.
        /// </summary>
        /// <param name="slot">The name of the slot to add.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the slot with specified name already exists.
        /// </exception>
        public ContainerSlot AddSlot(Slots slot)
        {
            if (HasSlot(slot))
            {
                throw new InvalidOperationException($"Slot '{slot}' already exists.");
            }

            Dirty();
            return SlotContainers[slot] = ContainerManagerComponent.Create<ContainerSlot>(GetSlotString(slot), Owner);
        }

        /// <summary>
        ///     Removes a slot from this inventory component.
        /// </summary>
        /// <remarks>
        ///     If the slot contains an item, the item is dropped.
        /// </remarks>
        /// <param name="slot">The name of the slot to remove.</param>
        public void RemoveSlot(Slots slot)
        {
            if (!HasSlot(slot))
            {
                throw new InvalidOperationException($"Slow '{slot}' does not exist.");
            }

            if (GetSlotItem(slot) != null && !Unequip(slot))
            {
                // TODO: Handle this potential failiure better.
                throw new InvalidOperationException(
                    "Unable to remove slot as the contained clothing could not be dropped");
            }

            SlotContainers.Remove(slot);
            Dirty();
        }

        /// <summary>
        ///     Checks whether a slot with the specified name exists.
        /// </summary>
        /// <param name="slot">The slot name to check.</param>
        /// <returns>True if the slot exists, false otherwise.</returns>
        public bool HasSlot(Slots slot)
        {
            return SlotContainers.ContainsKey(slot);
        }

        /// <summary>
        /// The underlying Container System just notified us that an entity was removed from it.
        /// We need to make sure we process that removed entity as being unequipped from the slot.
        /// </summary>
        private void ForceUnequip(IContainer container, IEntity entity)
        {
            // make sure this is one of our containers.
            // Technically the correct way would be to enumerate the possible slot names
            // comparing with this container, but I might as well put the dictionary to good use.
            if (!(container is ContainerSlot slot) || !SlotContainers.ContainsValue(slot))
                return;

            if (entity.TryGetComponent(out ItemComponent itemComp))
            {
                itemComp.RemovedFromSlot();
            }

            Dirty();
        }

        /// <summary>
        /// Message that tells us to equip or unequip items from the inventory slots
        /// </summary>
        /// <param name="msg"></param>
        private void HandleInventoryMessage(ClientInventoryMessage msg)
        {
            switch (msg.Updatetype)
            {
                case ClientInventoryUpdate.Equip:
                {
                    var hands = Owner.GetComponent<HandsComponent>();
                    var activeHand = hands.GetActiveHand;
                    if (activeHand != null && activeHand.Owner.TryGetComponent(out ItemComponent clothing))
                    {
                        hands.Drop(hands.ActiveIndex);
                        if (!Equip(msg.Inventoryslot, clothing, out var reason))
                        {
                            hands.PutInHand(clothing);

                            if (reason != null)
                                _serverNotifyManager.PopupMessageCursor(Owner, reason);
                        }
                    }
                    break;
                }
                case ClientInventoryUpdate.Use:
                {
                    var interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
                    var hands = Owner.GetComponent<HandsComponent>();
                    var activeHand = hands.GetActiveHand;
                    var itemContainedInSlot = GetSlotItem(msg.Inventoryslot);
                    if (itemContainedInSlot != null)
                    {
                        if (activeHand != null)
                        {
                            interactionSystem.Interaction(Owner, activeHand.Owner, itemContainedInSlot.Owner,
                                new GridCoordinates());
                        }
                        else if (Unequip(msg.Inventoryslot))
                        {
                            hands.PutInHand(itemContainedInSlot);
                        }
                    }
                    break;
                }
            }
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case ContainerContentsModifiedMessage msg:
                    if (msg.Removed)
                        ForceUnequip(msg.Container, msg.Entity);
                    break;

                default:
                    break;
            }
        }

        /// <inheritdoc />
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
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
                    if (item != null && item.Owner.TryGetComponent(out ServerStorageComponent storage))
                        storage.OpenStorageUI(Owner);
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            var list = new List<KeyValuePair<Slots, EntityUid>>();
            foreach (var (slot, container) in SlotContainers)
            {
                if (container.ContainedEntity != null)
                {
                    list.Add(new KeyValuePair<Slots, EntityUid>(slot, container.ContainedEntity.Uid));
                }
            }
            return new InventoryComponentState(list);
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            foreach (var slot in SlotContainers.Values.ToList())
            {
                foreach (var entity in slot.ContainedEntities)
                {
                    var exActs = entity.GetAllComponents<IExAct>().ToList();
                    foreach (var exAct in exActs)
                    {
                        exAct.OnExplosion(eventArgs);
                    }
                }
            }
        }
    }
}

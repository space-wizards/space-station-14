using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Clothing.Components;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.EffectBlocker;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;
using static Content.Shared.Inventory.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Server.Inventory.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedInventoryComponent))]
    public class InventoryComponent : SharedInventoryComponent, IExAct, IEffectBlocker
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        [ViewVariables] private readonly Dictionary<Slots, ContainerSlot> _slotContainers = new();

        private KeyValuePair<Slots, (EntityUid entity, bool fits)>? _hoverEntity;

        public IEnumerable<Slots> Slots => _slotContainers.Keys;

        public event Action? OnItemChanged;

        protected override void Initialize()
        {
            base.Initialize();

            foreach (var slotName in InventoryInstance.SlotMasks)
            {
                if (slotName != EquipmentSlotDefines.Slots.NONE)
                {
                    AddSlot(slotName);
                }
            }
        }

        public override float WalkSpeedModifier
        {
            get
            {
                var mod = 1f;
                foreach (var slot in _slotContainers.Values)
                {
                    if (slot.ContainedEntity != null)
                    {
                        foreach (var modifier in slot.ContainedEntity.GetAllComponents<IMoveSpeedModifier>())
                        {
                            mod *= modifier.WalkSpeedModifier;
                        }
                    }
                }

                return mod;
            }
        }

        public override float SprintSpeedModifier
        {
            get
            {
                var mod = 1f;
                foreach (var slot in _slotContainers.Values)
                {
                    if (slot.ContainedEntity != null)
                    {
                        foreach (var modifier in slot.ContainedEntity.GetAllComponents<IMoveSpeedModifier>())
                        {
                            mod *= modifier.SprintSpeedModifier;
                        }
                    }
                }

                return mod;
            }
        }

        bool IEffectBlocker.CanSlip()
        {
            return !TryGetSlotItem(EquipmentSlotDefines.Slots.SHOES, out ItemComponent? shoes) || EffectBlockerSystem.CanSlip(shoes.Owner);
        }

        protected override void OnRemove()
        {
            var slots = _slotContainers.Keys.ToList();

            foreach (var slot in slots)
            {
                if (TryGetSlotItem(slot, out ItemComponent? item))
                {
                    item.Owner.Delete();
                }

                RemoveSlot(slot);
            }

            base.OnRemove();
        }

        public IEnumerable<IEntity> GetAllHeldItems()
        {
            foreach (var (_, container) in _slotContainers)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    yield return entity;
                }
            }
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
        public ItemComponent? GetSlotItem(Slots slot)
        {
            return GetSlotItem<ItemComponent>(slot);
        }

        public IEnumerable<T?> LookupItems<T>() where T : Component
        {
            return _slotContainers.Values
                .SelectMany(x => x.ContainedEntities.Select(e => e.GetComponentOrNull<T>()))
                .Where(x => x != null);
        }

        public T? GetSlotItem<T>(Slots slot) where T : ItemComponent
        {
            if (!_slotContainers.ContainsKey(slot))
            {
                return null;
            }

            var containedEntity = _slotContainers[slot].ContainedEntity;
            if (containedEntity?.Deleted == true)
            {
                _slotContainers.Remove(slot);
                containedEntity = null;
                Dirty();
            }

            return containedEntity?.GetComponent<T>();
        }

        public bool TryGetSlotItem<T>(Slots slot, [NotNullWhen(true)] out T? itemComponent) where T : ItemComponent
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
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <param name="reason">The translated reason why the item cannot be equipped, if this function returns false. Can be null.</param>
        /// <returns>True if the item was successfully inserted, false otherwise.</returns>
        public bool Equip(Slots slot, ItemComponent item, bool mobCheck, [NotNullWhen(false)] out string? reason)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item),
                    "Clothing must be passed here. To remove some clothing from a slot, use Unequip()");
            }

            if (!CanEquip(slot, item, mobCheck, out reason))
            {
                return false;
            }

            var inventorySlot = _slotContainers[slot];
            if (!inventorySlot.Insert(item.Owner))
            {
                reason = Loc.GetString("inventory-component-on-equip-cannot");
                return false;
            }

            // TODO: Make clothing component not inherit ItemComponent, for fuck's sake.
            // TODO: Make clothing component not required for playing a sound on equip... Move it to its own component.
            if (mobCheck && item is ClothingComponent { EquipSound: {} equipSound })
            {
                SoundSystem.Play(Filter.Pvs(Owner), equipSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
            }

            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedInteraction(Owner, item.Owner, slot);

            OnItemChanged?.Invoke();

            Dirty();

            UpdateMovementSpeed();

            return true;
        }

        public bool Equip(Slots slot, ItemComponent item, bool mobCheck = true) =>
            Equip(slot, item, mobCheck, out var _);

        public bool Equip(Slots slot, IEntity entity, bool mobCheck = true) =>
            Equip(slot, entity.GetComponent<ItemComponent>(), mobCheck);

        /// <summary>
        ///     Checks whether an item can be put in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <param name="item">The item to check for.</param>
        /// <param name="reason">The translated reason why the item cannot be equiped, if this function returns false. Can be null.</param>
        /// <returns>True if the item can be inserted into the specified slot.</returns>
        public bool CanEquip(Slots slot, ItemComponent item, bool mobCheck, [NotNullWhen(false)] out string? reason)
        {
            var pass = false;
            reason = null;

            if (mobCheck && !EntitySystem.Get<ActionBlockerSystem>().CanEquip(Owner))
            {
                reason = Loc.GetString("inventory-component-can-equip-cannot");
                return false;
            }

            if (item is ClothingComponent clothing)
            {
                if (clothing.SlotFlags != SlotFlags.PREVENTEQUIP && (clothing.SlotFlags & SlotMasks[slot]) != 0)
                {
                    pass = true;
                }
                else
                {
                    reason = Loc.GetString("inventory-component-can-equip-does-not-fit");
                }
            }

            if (Owner.TryGetComponent(out IInventoryController? controller))
            {
                pass = controller.CanEquip(slot, item.Owner, pass, out var controllerReason);
                reason = controllerReason ?? reason;
            }

            if (!pass && reason == null)
            {
                reason = Loc.GetString("inventory-component-can-equip-cannot");
            }

            var canEquip = pass && _slotContainers[slot].CanInsert(item.Owner);

            if (!canEquip)
            {
                reason = Loc.GetString("inventory-component-can-equip-cannot");
            }

            return canEquip;
        }

        public bool CanEquip(Slots slot, ItemComponent item, bool mobCheck = true) =>
            CanEquip(slot, item, mobCheck, out var _);

        public bool CanEquip(Slots slot, IEntity entity, bool mobCheck = true) =>
            CanEquip(slot, entity.GetComponent<ItemComponent>(), mobCheck);

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        public bool Unequip(Slots slot, bool mobCheck = true)
        {
            if (!CanUnequip(slot, mobCheck))
            {
                return false;
            }

            var inventorySlot = _slotContainers[slot];
            var entity = inventorySlot.ContainedEntity;

            if (entity == null)
            {
                return false;
            }

            if (!inventorySlot.Remove(entity))
            {
                return false;
            }

            // TODO: The item should be dropped to the container our owner is in, if any.
            entity.Transform.AttachParentToContainerOrGrid();

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedInteraction(Owner, entity, slot);

            OnItemChanged?.Invoke();

            Dirty();

            UpdateMovementSpeed();

            return true;
        }

        private void UpdateMovementSpeed()
        {
            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? mod))
            {
                mod.RefreshMovementSpeedModifiers();
            }
        }

        public void ForceUnequip(Slots slot)
        {
            var inventorySlot = _slotContainers[slot];
            var entity = inventorySlot.ContainedEntity;
            if (entity == null)
            {
                return;
            }

            var item = entity.GetComponent<ItemComponent>();
            inventorySlot.ForceRemove(entity);

            var itemTransform = entity.Transform;

            itemTransform.AttachParentToContainerOrGrid();

            _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedInteraction(Owner, item.Owner, slot);

            OnItemChanged?.Invoke();

            Dirty();
        }

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        public bool CanUnequip(Slots slot, bool mobCheck = true)
        {
            if (mobCheck && !EntitySystem.Get<ActionBlockerSystem>().CanUnequip(Owner))
                return false;

            var inventorySlot = _slotContainers[slot];
            return inventorySlot.ContainedEntity != null && inventorySlot.CanRemove(inventorySlot.ContainedEntity);
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

            var container = ContainerHelpers.CreateContainer<ContainerSlot>(Owner, GetSlotString(slot));
            container.OccludesLight = false;
            _slotContainers[slot] = container;

            OnItemChanged?.Invoke();

            return _slotContainers[slot];
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
                throw new InvalidOperationException($"Slot '{slot}' does not exist.");
            }

            ForceUnequip(slot);

            var container = _slotContainers[slot];

            container.Shutdown();
            _slotContainers.Remove(slot);

            OnItemChanged?.Invoke();

            Dirty();
        }

        /// <summary>
        ///     Checks whether a slot with the specified name exists.
        /// </summary>
        /// <param name="slot">The slot name to check.</param>
        /// <returns>True if the slot exists, false otherwise.</returns>
        public bool HasSlot(Slots slot)
        {
            return _slotContainers.ContainsKey(slot);
        }

        /// <summary>
        /// The underlying Container System just notified us that an entity was removed from it.
        /// We need to make sure we process that removed entity as being unequipped from the slot.
        /// </summary>
        public void ForceUnequip(IContainer container, IEntity entity)
        {
            // make sure this is one of our containers.
            // Technically the correct way would be to enumerate the possible slot names
            // comparing with this container, but I might as well put the dictionary to good use.
            if (container is not ContainerSlot slot || !_slotContainers.ContainsValue(slot))
                return;

            if (entity.TryGetComponent(out ItemComponent? itemComp))
            {
                itemComp.RemovedFromSlot();
            }

            OnItemChanged?.Invoke();

            Dirty();
        }

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
                    var hands = Owner.GetComponent<HandsComponent>();
                    var activeHand = hands.ActiveHand;
                    var activeItem = hands.GetActiveHand;
                    if (activeHand != null && activeItem != null && activeItem.Owner.TryGetComponent(out ItemComponent? item))
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
                    var hands = Owner.GetComponent<HandsComponent>();
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
                    var hands = Owner.GetComponent<HandsComponent>();
                    var activeHand = hands.GetActiveHand;
                    if (activeHand != null && GetSlotItem(msg.Inventoryslot) == null)
                    {
                        var canEquip = CanEquip(msg.Inventoryslot, activeHand, true, out var reason);
                        _hoverEntity =
                            new KeyValuePair<Slots, (EntityUid entity, bool fits)>(msg.Inventoryslot,
                                (activeHand.Owner.Uid, canEquip));

                        Dirty();
                    }

                    break;
                }
            }
        }

        /// <inheritdoc />
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
                    if (item != null && item.Owner.TryGetComponent(out ServerStorageComponent? storage))
                        storage.OpenStorageUI(Owner);
                    break;
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var list = new List<KeyValuePair<Slots, EntityUid>>();
            foreach (var (slot, container) in _slotContainers)
            {
                if (container != null && container.ContainedEntity != null)
                {
                    list.Add(new KeyValuePair<Slots, EntityUid>(slot, container.ContainedEntity.Uid));
                }
            }

            var hover = _hoverEntity;
            _hoverEntity = null;

            return new InventoryComponentState(list, hover);
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            foreach (var slot in _slotContainers.Values.ToList())
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

        public override bool IsEquipped(IEntity item)
        {
            if (item == null) return false;
            foreach (var containerSlot in _slotContainers.Values)
            {
                // we don't want a recursive check here
                if (containerSlot.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

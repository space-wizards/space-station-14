using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using static Content.Shared.GameObjects.Components.Inventory.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedInventoryComponent))]
    public class InventoryComponent : SharedInventoryComponent, IExAct, IPressureProtection, IEffectBlocker
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        [ViewVariables] private readonly Dictionary<Slots, ContainerSlot> _slotContainers = new();

        private KeyValuePair<Slots, (EntityUid entity, bool fits)>? _hoverEntity;

        public IEnumerable<Slots> Slots => _slotContainers.Keys;

        public event Action? OnItemChanged;

        public override void Initialize()
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

        // Optimization: Cache this
        [ViewVariables]
        public float HighPressureMultiplier
        {
            get
            {
                var multiplier = 1f;

                foreach (var (slot, containerSlot) in _slotContainers)
                {
                    foreach (var entity in containerSlot.ContainedEntities)
                    {
                        foreach (var protection in entity.GetAllComponents<IPressureProtection>())
                        {
                            multiplier *= protection.HighPressureMultiplier;
                        }
                    }
                }

                return multiplier;
            }
        }

        // Optimization: Cache this
        [ViewVariables]
        public float LowPressureMultiplier
        {
            get
            {
                var multiplier = 1f;

                foreach (var (slot, containerSlot) in _slotContainers)
                {
                    foreach (var entity in containerSlot.ContainedEntities)
                    {
                        foreach (var protection in entity.GetAllComponents<IPressureProtection>())
                        {
                            multiplier *= protection.LowPressureMultiplier;
                        }
                    }
                }

                return multiplier;
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

        public override void OnRemove()
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
                reason = Loc.GetString("You can't equip this!");
                return false;
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

            if (mobCheck && !ActionBlockerSystem.CanEquip(Owner))
            {
                reason = Loc.GetString("You can't equip this!");
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
                    reason = Loc.GetString("This doesn't fit.");
                }
            }

            if (Owner.TryGetComponent(out IInventoryController? controller))
            {
                pass = controller.CanEquip(slot, item.Owner, pass, out var controllerReason);
                reason = controllerReason ?? reason;
            }

            if (!pass && reason == null)
            {
                reason = Loc.GetString("You can't equip this!");
            }

            var canEquip = pass && _slotContainers[slot].CanInsert(item.Owner);

            if (!canEquip)
            {
                reason = Loc.GetString("You can't equip this!");
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
            if (mobCheck && !ActionBlockerSystem.CanUnequip(Owner))
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
                throw new InvalidOperationException($"Slow '{slot}' does not exist.");
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
        private void ForceUnequip(IContainer container, IEntity entity)
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
                    if (!Owner.TryGetComponent(out HandsComponent? hands))
                        return;

                    if (!hands.TryGetActiveHeldEntity(out var heldEntity))
                        return;

                    if (!heldEntity.TryGetComponent(out ItemComponent? item))
                        return;

                    if (!hands.TryDropNoInteraction())
                        return;

                    if (!Equip(msg.Inventoryslot, item, true, out _))
                        hands.PutInHand(item);
                        
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
                            await interactionSystem.Interaction(Owner, activeHand.Owner, itemContainedInSlot.Owner,
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
        public override void HandleMessage(ComponentMessage message, IComponent? component)
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

        [Verb]
        private sealed class SetOutfitVerb : Verb<InventoryComponent>
        {
            public override bool RequireInteractionRange => false;
            public override bool BlockedByContainers => false;

            protected override void GetData(IEntity user, InventoryComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;
                if (!CanCommand(user))
                    return;

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Set Outfit");
                data.CategoryData = VerbCategories.Debug;
                data.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, InventoryComponent component)
            {
                if (!CanCommand(user))
                    return;

                var target = component.Owner;

                var entityId = target.Uid.ToString();

                var command = new SetOutfitCommand();
                var host = IoCManager.Resolve<IServerConsoleHost>();
                var args = new string[] {entityId};
                var session = user.PlayerSession();
                command.Execute(new ConsoleShell(host, session), $"{command.Command} {entityId}", args);
            }

            private static bool CanCommand(IEntity user)
            {
                var groupController = IoCManager.Resolve<IConGroupController>();
                return user.TryGetComponent<IActorComponent>(out var player) &&
                       groupController.CanCommand(player.playerSession, "setoutfit");
            }
        }
    }
}

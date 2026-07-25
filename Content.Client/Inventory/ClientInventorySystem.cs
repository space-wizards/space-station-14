using System.Linq;
using Content.Client.Clothing;
using Content.Client.Examine;
using Content.Client.Pointing;
using Content.Client.Verbs.UI;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed partial class ClientInventorySystem : InventorySystem
    {
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IUserInterfaceManager _ui = default!;
        [Dependency] private ClientClothingSystem _clothingVisualsSystem = default!;
        [Dependency] private ExamineSystem _examine = default!;
        [Dependency] private PointingSystem _pointing = default!;

        public Action<SlotData>? EntitySlotUpdate = null;
        public Action<SlotData>? OnSlotAdded = null;
        public Action<SlotData>? OnSlotRemoved = null;
        public Action<EntityUid, InventorySlotsComponent>? OnLinkInventorySlots = null;
        public Action? OnUnlinkInventory = null;
        public Action<SlotSpriteUpdate>? OnSpriteUpdate = null;

        private readonly Queue<(InventorySlotsComponent comp, EntityEventArgs args)> _equipEventsQueue = new();

        public override void Initialize()
        {
            UpdatesOutsidePrediction = true;
            base.Initialize();

            SubscribeLocalEvent<InventorySlotsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InventorySlotsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<InventoryComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<InventorySlotsComponent, DidEquipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));
            SubscribeLocalEvent<InventorySlotsComponent, DidUnequipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_equipEventsQueue.TryDequeue(out var tuple))
            {
                var (component, args) = tuple;

                switch (args)
                {
                    case DidEquipEvent equipped:
                        OnDidEquip(component, equipped);
                        break;
                    case DidUnequipEvent unequipped:
                        OnDidUnequip(component, unequipped);
                        break;
                    default:
                        throw new InvalidOperationException($"Received queued event of unknown type: {args.GetType()}");
                }
            }
        }

        private void OnDidUnequip(InventorySlotsComponent component, DidUnequipEvent args)
        {
            UpdateSlot(args.EquipTarget, component, args.Slot);
            if (args.EquipTarget != _playerManager.LocalEntity)
                return;
            var update = new SlotSpriteUpdate(null, args.SlotGroup, args.Slot, false);
            OnSpriteUpdate?.Invoke(update);
        }

        private void OnDidEquip(InventorySlotsComponent component, DidEquipEvent args)
        {
            UpdateSlot(args.EquipTarget, component, args.Slot);
            if (args.EquipTarget != _playerManager.LocalEntity)
                return;
            var update = new SlotSpriteUpdate(args.Equipment, args.SlotGroup, args.Slot,
                HasComp<StorageComponent>(args.Equipment));
            OnSpriteUpdate?.Invoke(update);
        }

        private void OnShutdown(EntityUid uid, InventoryComponent component, ComponentShutdown args)
        {
            if (TryComp(uid, out InventorySlotsComponent? inventorySlots))
            {
                foreach (var slot in component.Slots)
                {
                    TryRemoveSlotData((uid, inventorySlots), (SlotData)slot);
                }
            }

            if (uid == _playerManager.LocalEntity)
                OnUnlinkInventory?.Invoke();
        }

        private void OnPlayerDetached(EntityUid uid, InventorySlotsComponent component, LocalPlayerDetachedEvent args)
        {
            OnUnlinkInventory?.Invoke();
        }

        private void OnPlayerAttached(EntityUid uid, InventorySlotsComponent component, LocalPlayerAttachedEvent args)
        {
            OnLinkInventorySlots?.Invoke(uid, component);

            // TODO: Refactor client-side inventories. Code is VERY messy all over the UIController and this system.
            // Also StippableBUI has some duplication
            var enumerator = GetSlotEnumerator(uid);
            while (enumerator.NextItem(out var item))
            {
                if (!TryComp<InventorySlotBlockComponent>(item, out var comp))
                    continue;

                var blockedSlots = GetSlotEnumerator(uid, comp.Slots);
                while (blockedSlots.MoveNext(out var container))
                {
                    AddSlotBlocker(uid, container.ID, item);
                }
            }
        }

        protected override void OnInit(Entity<InventoryComponent> ent, ref ComponentInit args)
        {
            base.OnInit(ent, ref args);

            _clothingVisualsSystem.InitClothing(ent.Owner, ent.Comp);
        }

        [SubscribeLocalEvent]
        private void OnEquippedSlotBlocker(Entity<InventorySlotBlockComponent> ent, ref GotEquippedEvent args)
        {
            var enumerator = GetSlotEnumerator(args.EquipTarget, ent.Comp.Slots);
            while (enumerator.MoveNext(out var container))
            {
                AddSlotBlocker(args.EquipTarget, container.ID, ent);
            }
        }

        [SubscribeLocalEvent]
        private void OnUnequippedSlotBlocker(Entity<InventorySlotBlockComponent> ent, ref GotUnequippedEvent args)
        {
            var enumerator = GetSlotEnumerator(args.EquipTarget, ent.Comp.Slots);
            while (enumerator.MoveNext(out var container))
            {
                RemoveSlotBlocker(args.EquipTarget, container.ID, ent);
            }
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        public void ReloadInventory(InventorySlotsComponent? component = null)
        {
            var player = _playerManager.LocalEntity;
            if (player == null || !Resolve(player.Value, ref component, false))
            {
                return;
            }

            OnUnlinkInventory?.Invoke();
            OnLinkInventorySlots?.Invoke(player.Value, component);
        }

        public void SetSlotHighlight(EntityUid owner, InventorySlotsComponent component, string slotName, bool state)
        {
            var oldData = component.SlotData[slotName];
            var newData = component.SlotData[slotName] = new SlotData(oldData, state);
            if (owner == _playerManager.LocalEntity)
                EntitySlotUpdate?.Invoke(newData);
        }

        /// <summary>
        /// Adds a new slot blocker to the slot.
        /// </summary>
        /// <param name="ent">The entity containing the slot.</param>
        /// <param name="slotName">The name of the slot.</param>
        /// <param name="blocker">The blocker to add.</param>
        [PublicAPI]
        public void AddSlotBlocker(Entity<InventorySlotsComponent?> ent, string slotName, EntityUid blocker)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            var data = ent.Comp.SlotData[slotName];
            data.Blockers.Add(blocker);

            if (ent.Owner == _playerManager.LocalEntity)
                EntitySlotUpdate?.Invoke(data);
        }

        /// <summary>
        /// Removes a slot blocker from the slot.
        /// </summary>
        /// <param name="ent">The entity containing the slot.</param>
        /// <param name="slotName">The name of the slot.</param>
        /// <param name="blocker">The blocker to remove.</param>
        [PublicAPI]
        public void RemoveSlotBlocker(Entity<InventorySlotsComponent?> ent, string slotName, EntityUid blocker)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            var data = ent.Comp.SlotData[slotName];
            data.Blockers.Remove(blocker);

            if (ent.Owner == _playerManager.LocalEntity)
                EntitySlotUpdate?.Invoke(data);
        }

        public void UpdateSlot(EntityUid owner, InventorySlotsComponent component, string slotName,
            bool? blocked = null, bool? highlight = null)
        {
            // The slot might have been removed when changing templates, which can cause items to be dropped.
            if (!component.SlotData.TryGetValue(slotName, out var oldData))
                return;

            var newHighlight = oldData.Highlighted;
            var newBlocked = oldData.Blocked;

            if (blocked != null)
                newBlocked = blocked.Value;

            if (highlight != null)
                newHighlight = highlight.Value;

            var newData = component.SlotData[slotName] =
                new SlotData(component.SlotData[slotName], newHighlight, newBlocked);
            if (owner == _playerManager.LocalEntity)
                EntitySlotUpdate?.Invoke(newData);
        }

        public bool TryAddSlotData(Entity<InventorySlotsComponent> ent, SlotData newSlotData)
        {
            if (!ent.Comp.SlotData.TryAdd(newSlotData.SlotName, newSlotData))
                return false;

            if (TryGetSlotContainer(ent.Owner, newSlotData.SlotName, out var newContainer, out _))
                ent.Comp.SlotData[newSlotData.SlotName].Container = newContainer;

            if (ent.Owner == _playerManager.LocalEntity)
                OnSlotAdded?.Invoke(newSlotData);

            return true;
        }

        public bool TryRemoveSlotData(Entity<InventorySlotsComponent> ent, SlotData removedSlotData)
        {
            if (!ent.Comp.SlotData.Remove(removedSlotData.SlotName))
                return false;

            if (ent.Owner == _playerManager.LocalEntity)
                OnSlotRemoved?.Invoke(removedSlotData);

            return true;
        }

        public void UIInventoryActivate(string slot)
        {
            RaisePredictiveEvent(new UseSlotNetworkMessage(slot));
        }

        public void UIInventoryStorageActivate(string slot)
        {
            RaisePredictiveEvent(new OpenSlotStorageNetworkMessage(slot));
        }

        public void UIInventoryExamine(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            _examine.DoExamine(item.Value);
        }

        public void UIInventoryOpenContextMenu(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            _ui.GetUIController<VerbMenuUIController>().OpenVerbMenu(item.Value);
        }

        public void UIInventoryActivateItem(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            RaisePredictiveEvent(
                new InteractInventorySlotEvent(GetNetEntity(item.Value), altInteract: false));
        }

        public void UIInventoryAltActivateItem(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            RaisePredictiveEvent(new InteractInventorySlotEvent(GetNetEntity(item.Value), altInteract: true));
        }

        /// <summary>
        /// Points at an item in the inventory
        /// </summary>
        /// <param name="slot">The slot to point at</param>
        /// <param name="uid">The inventory entity containing the slot</param>
        public void UIInventoryPointAt(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            _pointing.TryPointAtEntity(GetNetEntity(item.Value));
        }

        protected override void UpdateInventoryTemplate(Entity<InventoryComponent> ent)
        {
            base.UpdateInventoryTemplate(ent);

            if (!TryComp<InventorySlotsComponent>(ent, out var inventorySlots))
                return;

            List<SlotData> slotDataToRemove = new(); // don't modify dict while iterating

            foreach (var slotData in inventorySlots.SlotData.Values)
            {
                if (!ent.Comp.Slots.Any(s => s.Name == slotData.SlotName))
                    slotDataToRemove.Add(slotData);
            }

            // remove slots that are no longer in the new template
            foreach (var slotData in slotDataToRemove)
            {
                TryRemoveSlotData((ent.Owner, inventorySlots), slotData);
            }

            // update existing slots or add them if they don't exist yet
            foreach (var slot in ent.Comp.Slots)
            {
                if (inventorySlots.SlotData.TryGetValue(slot.Name, out var slotData))
                    slotData.SlotDef = slot;
                else
                    TryAddSlotData((ent.Owner, inventorySlots), (SlotData)slot);
            }

            if (ent.Owner == _playerManager.LocalEntity)
                ReloadInventory(inventorySlots);
        }

        public sealed class SlotData
        {
            [ViewVariables] public SlotDefinition SlotDef;
            [ViewVariables] public EntityUid? HeldEntity => Container?.ContainedEntity;
            [ViewVariables] public bool Blocked;
            [ViewVariables] public bool Highlighted;
            [ViewVariables] public ContainerSlot? Container;
            [ViewVariables] public List<EntityUid> Blockers = [];
            [ViewVariables] public bool HasSlotGroup => SlotDef.SlotGroup != "Default";
            [ViewVariables] public Vector2i ButtonOffset => SlotDef.UIWindowPosition;
            [ViewVariables] public string SlotName => SlotDef.Name;
            [ViewVariables] public bool ShowInWindow => SlotDef.ShowInWindow;
            [ViewVariables] public string SlotGroup => SlotDef.SlotGroup;
            [ViewVariables] public string SlotDisplayName => SlotDef.DisplayName;
            [ViewVariables] public string TextureName => "Slots/" + SlotDef.TextureName;
            [ViewVariables] public string FullTextureName => SlotDef.FullTextureName;

            public SlotData(SlotDefinition slotDef, ContainerSlot? container = null, bool highlighted = false,
                bool blocked = false)
            {
                SlotDef = slotDef;
                Highlighted = highlighted;
                Blocked = blocked;
                Container = container;
            }

            public SlotData(SlotData oldData, bool highlighted = false, bool blocked = false)
            {
                SlotDef = oldData.SlotDef;
                Highlighted = highlighted;
                Container = oldData.Container;
                Blocked = blocked;
                Blockers = oldData.Blockers;
            }

            /// <summary>
            /// Returns whether this slot is blocked.
            /// </summary>
            /// <returns>Whether this slot is blocked.</returns>
            public bool IsBlocked()
            {
                return Blockers.Count > 0 || Blocked;
            }

            public static implicit operator SlotData(SlotDefinition s)
            {
                return new SlotData(s);
            }

            public static implicit operator SlotDefinition(SlotData s)
            {
                return s.SlotDef;
            }
        }

        public readonly record struct SlotSpriteUpdate(
            EntityUid? Entity,
            string Group,
            string Name,
            bool ShowStorage
        );
    }
}

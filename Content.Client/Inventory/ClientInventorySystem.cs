using System.Linq;
using Content.Client.Clothing;
using Content.Client.Examine;
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
    public sealed class ClientInventorySystem : InventorySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;
        [Dependency] private readonly ClientClothingSystem _clothingVisualsSystem = default!;
        [Dependency] private readonly ExamineSystem _examine = default!;

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
            UpdateSlot(args.Equipee, component, args.Slot);
            if (args.Equipee != _playerManager.LocalEntity)
                return;
            var update = new SlotSpriteUpdate(null, args.SlotGroup, args.Slot, false);
            OnSpriteUpdate?.Invoke(update);
        }

        private void OnDidEquip(InventorySlotsComponent component, DidEquipEvent args)
        {
            UpdateSlot(args.Equipee, component, args.Slot);
            if (args.Equipee != _playerManager.LocalEntity)
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
        }

        protected override void OnInit(Entity<InventoryComponent> ent, ref ComponentInit args)
        {
            base.OnInit(ent, ref args);

            _clothingVisualsSystem.InitClothing(ent.Owner, ent.Comp);
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

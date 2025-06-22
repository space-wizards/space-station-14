using System.Diagnostics;
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
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : InventorySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

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
            SubscribeLocalEvent<InventoryComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<InventorySlotsComponent, DidEquipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));
            SubscribeLocalEvent<InventorySlotsComponent, DidUnequipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));
        }

        private void OnHandleState(Entity<InventoryComponent> ent, ref ComponentHandleState args)
        {
            if (args.Current is not InventoryComponentState state)
                return;

            ent.Comp.TemplateId = state.Template;
            ent.Comp.SpeciesId = state.Species;

            UpdateInventoryTemplate(ent);

            Log.Debug("State received");
        }

        private void UpdateInventoryTemplate(Entity<InventoryComponent> ent)
        {
            if (!TryComp<InventorySlotsComponent>(ent, out var slots))
                return;

            if (!_proto.TryIndex(ent.Comp.TemplateId, out var index))
                return;

            foreach (var slot in ent.Comp.Slots)
            {
                Log.Debug("Pre-Found slot: " + slot.Name + " " + Array.IndexOf(ent.Comp.Slots, slot));
            }

            foreach (var slot in ent.Comp.Containers)
            {
                Log.Debug("Pre-Found container: " + slot.ID + " " + Array.IndexOf(ent.Comp.Containers, slot));
            }

            ent.Comp.Slots = index.Slots;
            // TODO: Find a way to recreate Containers. Enumerator shits itself without them

            foreach (var slot in slots.SlotData)
            {
                Log.Debug("[Rem] Checking slot: " + slot.Key);
                if (index.Slots.Any(s => s.Name == slot.Key))
                {
                    Log.Debug("Slot found, skipping.");
                    continue;
                }
                Log.Debug("Attempting to remove: " + slot.Key);

                TryRemoveSlotDef(ent, slots, slot.Value);
            }

            foreach (var slot in index.Slots)
            {
                Log.Debug("[Add] Checking slot: " + slot.Name);
                if (slots.SlotData.Any(s => s.Key == slot.Name))
                    continue;

                Log.Debug("Attempting to add: " + slot.Name);

                TryAddSlotDef(ent, slots, slot);
            }

            foreach (var slot in ent.Comp.Slots)
            {
                Log.Debug("Pre-Update Found slot: " + slot.Name + " " + Array.IndexOf(ent.Comp.Slots, slot));
            }

            foreach (var slot in ent.Comp.Containers)
            {
                Log.Debug("Pre-Update Found container: " + Array.IndexOf(ent.Comp.Containers, slot));
            }

            ent.Comp.Containers = new ContainerSlot[ent.Comp.Slots.Length];

            foreach (var slot in ent.Comp.Containers)
            {
                Log.Debug("Post-Update Found container: " + Array.IndexOf(ent.Comp.Containers, slot));
            }

            for (var i = 0; i < ent.Comp.Containers.Length; i++)
            {
                Log.Debug("Checking slot index " + i + " for " + ent.Comp.Slots[i].Name);
                var slot = ent.Comp.Slots[i];
                if (!TryGetSlotContainer(ent.Owner, slot.Name, out var slotContainer, out var _))
                    return;

                slotContainer.OccludesLight = false;
                // ent.Comp.Containers[i] = slotContainer; TODO: This somehow causes Out of Index. Everything here is out of index GRAHHH.
            }

            foreach (var slot in ent.Comp.Slots)
            {
                Log.Debug("Found slot: " + slot.Name);
            }

            foreach (var slot in ent.Comp.Containers)
            {
                Log.Debug("Found container: " + slot.ID);
            }

            ReloadInventory();
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
            if (!TryComp(uid, out InventorySlotsComponent? inventorySlots))
                return;

            foreach (var slot in component.Slots)
            {
                TryRemoveSlotDef(uid, inventorySlots, slot);
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
            if (TryGetSlots(uid, out var definitions))
            {
                foreach (var definition in definitions)
                {
                    if (!TryGetSlotContainer(uid, definition.Name, out var container, out _))
                        continue;

                    if (!component.SlotData.TryGetValue(definition.Name, out var data))
                    {
                        data = new SlotData(definition);
                        component.SlotData[definition.Name] = data;
                    }

                    data.Container = container;
                }
            }

            OnLinkInventorySlots?.Invoke(uid, component);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        protected override void OnInit(EntityUid uid, InventoryComponent component, ComponentInit args)
        {
            base.OnInit(uid, component, args);
            _clothingVisualsSystem.InitClothing(uid, component);

            if (!TryComp(uid, out InventorySlotsComponent? inventorySlots))
                return;

            foreach (var slot in component.Slots)
            {
                TryAddSlotDef(uid, inventorySlots, slot);
            }
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
            if (!HasSlot(owner, slotName)) // TODO: This somehow breaks sprite updating. But without it it crashes. God save me.
                return;

            var oldData = component.SlotData[slotName];
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

        public bool TryAddSlotDef(EntityUid owner, InventorySlotsComponent component, SlotDefinition newSlotDef)
        {
            Log.Debug("Attempting to add: " + newSlotDef.Name);
            SlotData newSlotData = newSlotDef; //convert to slotData
            if (!component.SlotData.TryAdd(newSlotDef.Name, newSlotData))
                return false;

            if (owner == _playerManager.LocalEntity)
                OnSlotAdded?.Invoke(newSlotData);

            Log.Debug("Adding def client:" + newSlotDef.Name);
            return true;
        }

        public bool TryRemoveSlotDef(EntityUid owner, InventorySlotsComponent component, SlotDefinition newSlotDef)
        {
            SlotData newSlotData = newSlotDef; //convert to slotData
            if (!component.SlotData.Remove(newSlotDef.Name))
            {
                return false;
            }

            Log.Debug("Removing def client:" + newSlotDef.Name);

            if (owner == _playerManager.LocalEntity)
                OnSlotRemoved?.Invoke(newSlotData);
            return true;
        }

        public void UIInventoryActivate(string slot)
        {
            EntityManager.RaisePredictiveEvent(new UseSlotNetworkMessage(slot));
        }

        public void UIInventoryStorageActivate(string slot)
        {
            EntityManager.RaisePredictiveEvent(new OpenSlotStorageNetworkMessage(slot));
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

            EntityManager.RaisePredictiveEvent(
                new InteractInventorySlotEvent(GetNetEntity(item.Value), altInteract: false));
        }

        public void UIInventoryAltActivateItem(string slot, EntityUid uid)
        {
            if (!TryGetSlotEntity(uid, slot, out var item))
                return;

            EntityManager.RaisePredictiveEvent(new InteractInventorySlotEvent(GetNetEntity(item.Value), altInteract: true));
        }

        public sealed class SlotData
        {
            public SlotDefinition SlotDef;
            public EntityUid? HeldEntity => Container?.ContainedEntity;
            public bool Blocked;
            public bool Highlighted;

            [ViewVariables]
            public ContainerSlot? Container;
            public bool HasSlotGroup => SlotDef.SlotGroup != "Default";
            public Vector2i ButtonOffset => SlotDef.UIWindowPosition;
            public string SlotName => SlotDef.Name;
            public bool ShowInWindow => SlotDef.ShowInWindow;
            public string SlotGroup => SlotDef.SlotGroup;
            public string SlotDisplayName => SlotDef.DisplayName;
            public string TextureName => "Slots/" + SlotDef.TextureName;
            public string FullTextureName => SlotDef.FullTextureName;

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

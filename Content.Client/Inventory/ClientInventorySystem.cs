using Content.Client.Clothing;
using Content.Client.Examine;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : InventorySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
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

            SubscribeLocalEvent<InventorySlotsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InventorySlotsComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<InventoryComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<InventorySlotsComponent, DidEquipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));
            SubscribeLocalEvent<InventorySlotsComponent, DidUnequipEvent>((_, comp, args) =>
                _equipEventsQueue.Enqueue((comp, args)));

            SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);
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

        private void OnUseInHand(EntityUid uid, ClothingComponent component, UseInHandEvent args)
        {
            if (args.Handled || !component.QuickEquip)
                return;

            QuickEquip(uid, component, args);
        }

        private void OnDidUnequip(InventorySlotsComponent component, DidUnequipEvent args)
        {
            UpdateSlot(args.Equipee, component, args.Slot);
            if (args.Equipee != _playerManager.LocalPlayer?.ControlledEntity)
                return;
            var update = new SlotSpriteUpdate(null, args.SlotGroup, args.Slot, false);
            OnSpriteUpdate?.Invoke(update);
        }

        private void OnDidEquip(InventorySlotsComponent component, DidEquipEvent args)
        {
            UpdateSlot(args.Equipee, component, args.Slot);
            if (args.Equipee != _playerManager.LocalPlayer?.ControlledEntity)
                return;
            var update = new SlotSpriteUpdate(args.Equipment, args.SlotGroup, args.Slot,
                HasComp<StorageComponent>(args.Equipment));
            OnSpriteUpdate?.Invoke(update);
        }

        private void OnShutdown(EntityUid uid, InventoryComponent component, ComponentShutdown args)
        {
            if (component.Owner != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            OnUnlinkInventory?.Invoke();
        }

        private void OnPlayerDetached(EntityUid uid, InventorySlotsComponent component, PlayerDetachedEvent args)
        {
            OnUnlinkInventory?.Invoke();
        }

        private void OnPlayerAttached(EntityUid uid, InventorySlotsComponent component, PlayerAttachedEvent args)
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

            if (!_prototypeManager.TryIndex(component.TemplateId, out InventoryTemplatePrototype? invTemplate) ||
                !TryComp(uid, out InventorySlotsComponent? inventorySlots))
            {
                return;
            }

            foreach (var slot in invTemplate.Slots)
            {
                TryAddSlotDef(uid, inventorySlots, slot);
            }
        }

        public void ReloadInventory(InventorySlotsComponent? component = null)
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;
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
            if (owner == _playerManager.LocalPlayer?.ControlledEntity)
                EntitySlotUpdate?.Invoke(newData);
        }

        public void UpdateSlot(EntityUid owner, InventorySlotsComponent component, string slotName,
            bool? blocked = null, bool? highlight = null)
        {
            var oldData = component.SlotData[slotName];
            var newHighlight = oldData.Highlighted;
            var newBlocked = oldData.Blocked;

            if (blocked != null)
                newBlocked = blocked.Value;

            if (highlight != null)
                newHighlight = highlight.Value;

            var newData = component.SlotData[slotName] =
                new SlotData(component.SlotData[slotName], newHighlight, newBlocked);
            if (owner == _playerManager.LocalPlayer?.ControlledEntity)
                EntitySlotUpdate?.Invoke(newData);
        }

        public bool TryAddSlotDef(EntityUid owner, InventorySlotsComponent component, SlotDefinition newSlotDef)
        {
            SlotData newSlotData = newSlotDef; //convert to slotData
            if (!component.SlotData.TryAdd(newSlotDef.Name, newSlotData))
                return false;

            if (owner == _playerManager.LocalPlayer?.ControlledEntity)
                OnSlotAdded?.Invoke(newSlotData);
            return true;
        }

        public void RemoveSlotDef(EntityUid owner, InventorySlotsComponent component, SlotData slotData)
        {
            if (component.SlotData.Remove(slotData.SlotName))
            {
                if (owner == _playerManager.LocalPlayer?.ControlledEntity)
                    OnSlotRemoved?.Invoke(slotData);
            }
        }

        public void RemoveSlotDef(EntityUid owner, InventorySlotsComponent component, string slotName)
        {
            if (!component.SlotData.TryGetValue(slotName, out var slotData))
                return;

            component.SlotData.Remove(slotName);

            if (owner == _playerManager.LocalPlayer?.ControlledEntity)
                OnSlotRemoved?.Invoke(slotData);
        }

        // TODO hud refactor This should also live in a UI Controller
        private void HoverInSlotButton(EntityUid uid, string slot, SlotControl control,
            InventoryComponent? inventoryComponent = null, HandsComponent? hands = null)
        {
            if (!Resolve(uid, ref inventoryComponent))
                return;

            if (!Resolve(uid, ref hands, false))
                return;

            if (hands.ActiveHandEntity is not EntityUid heldEntity)
                return;

            if (!TryGetSlotContainer(uid, slot, out var containerSlot, out var slotDef, inventoryComponent))
                return;
        }

        public void UIInventoryActivate(string slot)
        {
            EntityManager.RaisePredictiveEvent(new UseSlotNetworkMessage(slot));
        }

        public void UIInventoryStorageActivate(string slot)
        {
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(new OpenSlotStorageNetworkMessage(slot));
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
            public readonly SlotDefinition SlotDef;
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

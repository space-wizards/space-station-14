using Content.Client.Clothing;
using Content.Shared.Input;
using Content.Client.UserInterface.Controllers;
using Content.Client.UserInterface.Controls;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Events;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : InventorySystem
    {
        //[Dependency] private readonly IHudManager _hudManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly ClothingSystem _clothingSystem = default!;

        public Action<SlotData>? EntitySlotUpdate = null;
        public Action<SlotData>? OnSlotAdded = null;
        public Action<SlotData>? OnSlotRemoved = null;
        public Action? OnOpenInventory = null;
        public Action<ClientInventoryComponent?>? OnLinkInventory = null;
        public Action<ClientInventoryComponent?>? OnUnlinkInventory = null;

        private readonly Queue<(ClientInventoryComponent comp, DidEquipEvent args)> _equipEventsQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();

            SubscribeLocalEvent<ClientInventoryComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ClientInventoryComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<ClientInventoryComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ClientInventoryComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<ClientInventoryComponent, DidEquipEvent>((_, comp, args) => _equipEventsQueue.Enqueue((comp, args)));
            SubscribeLocalEvent<ClientInventoryComponent, DidUnequipEvent>(OnDidUnequip);

            SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_equipEventsQueue.TryDequeue(out var tuple))
            {
                var (component, args) = tuple;
                OnDidEquip(component, args);
            }
        }

        private void OnUseInHand(EntityUid uid, ClothingComponent component, UseInHandEvent args)
        {
            if (args.Handled || !component.QuickEquip)
                return;

            QuickEquip(uid, component, args);
        }

        private void OnDidUnequip(EntityUid uid, ClientInventoryComponent component, DidUnequipEvent args)
        {
            UpdateSlot(component, args.Slot);
        }

        private void OnDidEquip(ClientInventoryComponent component, DidEquipEvent args)
        {
            UpdateSlot(component, args.Slot);
        }

        private void OnPlayerDetached(EntityUid uid, ClientInventoryComponent component, PlayerDetachedEvent? args = null)
        {
            OnUnlinkInventory?.Invoke(null);
        }

        private void OnShutdown(EntityUid uid, ClientInventoryComponent component, ComponentShutdown args)
        {
            OnPlayerDetached(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, ClientInventoryComponent component, PlayerAttachedEvent args)
        {
            OnLinkInventory?.Invoke(component);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        private void OnInit(EntityUid uid, ClientInventoryComponent component, ComponentInit args)
        {
            _clothingSystem.InitClothing(uid, component);
            if (!_prototypeManager.TryIndex(component.TemplateId, out InventoryTemplatePrototype? invTemplate)) return;
            foreach (var slot in invTemplate.Slots)
            {
                TryAddSlotDef(component, slot);
            }
        }
        public void SetSlotHighlight(ClientInventoryComponent component, string slotName, bool state)
        {
            var oldData = component.SlotData[slotName];
            var newData = component.SlotData[slotName] = new SlotData(oldData, state);
            EntitySlotUpdate?.Invoke(newData);
        }
        public void UpdateSlot(ClientInventoryComponent component,string slotName,bool? blocked = null, bool? highlight = null)
        {

            var oldData = component.SlotData[slotName];
            var newHighlight = oldData.Highlighted;
            var newBlocked = oldData.Blocked;
            if (blocked != null) newBlocked = blocked.Value;
            if (highlight != null) newHighlight = highlight.Value;
            var newData = component.SlotData[slotName] = new SlotData(component.SlotData[slotName], newHighlight, newBlocked);
            EntitySlotUpdate?.Invoke(newData);
        }

        public bool TryAddSlotDef(ClientInventoryComponent component,SlotDefinition newSlotDef)
        {
            SlotData newSlotData = newSlotDef; //convert to slotData
            if (!component.SlotData.TryAdd(newSlotDef.Name, newSlotData)) return false;
            OnSlotAdded?.Invoke(newSlotData);
            return true;
        }
        public void RemoveSlotDef(ClientInventoryComponent component, SlotData slotData)
        {
            if (component.SlotData.Remove(slotData.SlotName))
            {
                OnSlotRemoved?.Invoke(slotData);
            }
        }
        public void RemoveSlotDef(ClientInventoryComponent component, string slotName)
        {
            if (!component.SlotData.TryGetValue(slotName, out var slotData)) return;
                component.SlotData.Remove(slotName);
                OnSlotRemoved?.Invoke(slotData);

        }

        //This should also live in a UI Controller
        private void HoverInSlotButton(EntityUid uid, string slot, ItemSlotControl control, InventoryComponent? inventoryComponent = null, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref inventoryComponent))
                return;

            if (!Resolve(uid, ref hands, false))
                return;

            if (hands.ActiveHandEntity is not EntityUid heldEntity)
                return;

            if(!TryGetSlotContainer(uid, slot, out var containerSlot, out var slotDef, inventoryComponent))
                return;

            //HoverInSlot(button, heldEntity,
             //   CanEquip(uid, heldEntity, slot, out _, slotDef, inventoryComponent) &&
             //   containerSlot.CanInsert(heldEntity, EntityManager));
        }
        private void HandleOpenInventoryMenu()
        {
            OnOpenInventory?.Invoke();
        }


        //TODO: This should live in a UI controller
        private void HandleSlotButtonPressed(EntityUid uid, string slot, ItemSlotControl control,
            GUIBoundKeyEventArgs args)
        {
            if (TryGetSlotEntity(uid, slot, out var itemUid))
                return;

            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            // only raise event if either itemUid is not null, or the user is holding something
            if (itemUid != null || TryComp(uid, out SharedHandsComponent? hands) && hands.ActiveHandEntity != null)
                EntityManager.RaisePredictiveEvent(new UseSlotNetworkMessage(slot));
        }


        public struct SlotData
        {
            public readonly SlotDefinition SlotDef;
            public EntityUid? HeldEntity => Container?.ContainedEntity;
            public bool Blocked ;
            public bool Highlighted;
            public ContainerSlot? Container;
            public bool HasSlotGroup => SlotDef.SlotGroup != "";
            public string SlotName => SlotDef.Name;
            public string SlotGroup => SlotDef.SlotGroup;
            public string SlotDisplayName => SlotDef.DisplayName;
            public string TextureName => "slots/"+SlotDef.TextureName;
            public SlotData(SlotDefinition slotDef,ContainerSlot? container = null, bool highlighted = false, bool blocked = false)
            {
                SlotDef = slotDef;
                Highlighted = highlighted;
                Blocked = blocked;
                Container = container;
            }

            public SlotData(SlotData oldData,bool highlighted = false,bool blocked = false)
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
    }
}

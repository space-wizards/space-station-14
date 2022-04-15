using System.Diagnostics.CodeAnalysis;
using Content.Client.Clothing;
using Content.Client.HUD;
using Content.Shared.Input;
using Content.Client.Items.Managers;
using Content.Client.Items.UI;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Events;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed partial class ClientInventorySystem : InventorySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly ClothingSystem _clothingSystem = default!;

        public const int ButtonSize = 64;
        private const int ButtonSeparation = 4;
        private const int RightSeparation = 2;

        /// <summary>
        /// Stores delegates used to create controls for a given <see cref="InventoryTemplatePrototype"/>.
        /// </summary>
        private readonly
            Dictionary<string, Func<EntityUid, Dictionary<string, (ItemSlotButton, ItemSlotButton)>, (DefaultWindow window, Control bottomLeft, Control bottomRight, Control
                topQuick)>>
            _uiGenerateDelegates = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeInventorySlots();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();

            SubscribeLocalEvent<ClientInventoryComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ClientInventoryComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<ClientInventoryComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ClientInventoryComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<ClientInventoryComponent, DidEquipEvent>(OnDidEquip);
            SubscribeLocalEvent<ClientInventoryComponent, DidUnequipEvent>(OnDidUnequip);

            SubscribeLocalEvent<ClothingComponent, UseInHandEvent>(OnUseInHand);

            _config.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);
        }

        private void OnUseInHand(EntityUid uid, ClothingComponent component, UseInHandEvent args)
        {
            if (args.Handled || !component.QuickEquip)
                return;

            QuickEquip(uid, component, args);
        }

        private void OnDidUnequip(EntityUid uid, ClientInventoryComponent component, DidUnequipEvent args)
        {
            UpdateComponentUISlot(uid, args.Slot, null, component);
        }

        private void OnDidEquip(EntityUid uid, ClientInventoryComponent component, DidEquipEvent args)
        {
            UpdateComponentUISlot(uid, args.Slot, args.Equipment, component);
        }

        private void UpdateComponentUISlot(EntityUid uid, string slot, EntityUid? item, ClientInventoryComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!component.SlotButtons.TryGetValue(slot, out var buttons) &&
                (!TryComp<ClientInventorySlotComponent>(uid, out var invSlotComp) ||
                 !invSlotComp.SlotButtons.TryGetValue(slot, out buttons)))
                return;

            _itemSlotManager.SetItemSlot(buttons.hudButton, item);
            _itemSlotManager.SetItemSlot(buttons.windowButton, item);
        }

        private void OnPlayerDetached(EntityUid uid, ClientInventoryComponent component, PlayerDetachedEvent? args = null)
        {
            if(!component.AttachedToGameHud) return;

            _gameHud.InventoryButtonVisible = false;
            _gameHud.BottomLeftInventoryQuickButtonContainer.RemoveChild(component.BottomLeftButtons);
            _gameHud.BottomRightInventoryQuickButtonContainer.RemoveChild(component.BottomRightButtons);
            _gameHud.TopInventoryQuickButtonContainer.RemoveChild(component.TopQuickButtons);
            component.AttachedToGameHud = false;
        }

        private void OnShutdown(EntityUid uid, ClientInventoryComponent component, ComponentShutdown args)
        {
            OnPlayerDetached(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, ClientInventoryComponent component, PlayerAttachedEvent args)
        {
            if(component.AttachedToGameHud) return;

            _gameHud.InventoryButtonVisible = true;
            _gameHud.BottomLeftInventoryQuickButtonContainer.AddChild(component.BottomLeftButtons);
            _gameHud.BottomRightInventoryQuickButtonContainer.AddChild(component.BottomRightButtons);
            _gameHud.TopInventoryQuickButtonContainer.AddChild(component.TopQuickButtons);
            component.AttachedToGameHud = true;
        }

        private void UpdateHudTheme(int obj)
        {
            if (!_gameHud.ValidateHudTheme(obj))
            {
                return;
            }

            foreach (var inventoryComponent in EntityManager.EntityQuery<ClientInventoryComponent>(true))
            {
                foreach (var (_, btn) in inventoryComponent.SlotButtons)
                {
                    btn.hudButton.RefreshTextures(_gameHud);
                    btn.windowButton.RefreshTextures(_gameHud);
                }
            }

            foreach (var inventorySlotComponent in EntityManager.EntityQuery<ClientInventorySlotComponent>(true))
            {
                foreach (var (_, btn) in inventorySlotComponent.SlotButtons)
                {
                    btn.hudButton.RefreshTextures(_gameHud);
                    btn.windowButton.RefreshTextures(_gameHud);
                }
            }
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            _config.UnsubValueChanged(CCVars.HudTheme, UpdateHudTheme);
            base.Shutdown();
        }

        private void OnInit(EntityUid uid, ClientInventoryComponent component, ComponentInit args)
        {
            _clothingSystem.InitClothing(uid, component);

            if (!TryGetUIElements(uid, out var window, out var bottomLeft, out var bottomRight, out var topQuick,
                    component))
                return;

            if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
            {
                InitSlotButtons(uid, component.SlotButtons, component, containerManager);
            }

            component.InventoryWindow = window;
            component.BottomLeftButtons = bottomLeft;
            component.BottomRightButtons = bottomRight;
            component.TopQuickButtons = topQuick;
        }

        private void InitSlotButtons(EntityUid uid, Dictionary<string, (ItemSlotButton, ItemSlotButton)> slotButtons, ClientInventoryComponent component, ContainerManagerComponent containerManager)
        {
            foreach (var (slot, button) in slotButtons)
            {
                if (!TryGetSlotEntity(uid, slot, out var entity, component, containerManager))
                    continue;

                _itemSlotManager.SetItemSlot(button.Item1, entity);
                _itemSlotManager.SetItemSlot(button.Item2, entity);
            }
        }

        private void HoverInSlotButton(EntityUid uid, string slot, ItemSlotButton button, InventoryComponent? inventoryComponent = null, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref inventoryComponent))
                return;

            if (!Resolve(uid, ref hands, false))
                return;

            if (hands.ActiveHandEntity is not EntityUid heldEntity)
                return;

            if(!TryGetSlotContainer(uid, slot, out var containerSlot, out var slotDef, inventoryComponent))
                return;

            _itemSlotManager.HoverInSlot(button, heldEntity,
                CanEquip(uid, heldEntity, slot, out _, slotDef, inventoryComponent) &&
                containerSlot.CanInsert(heldEntity, EntityManager));
        }

        private void HandleSlotButtonPressed(EntityUid uid, string slot, ItemSlotButton button,
            GUIBoundKeyEventArgs args)
        {
            if (TryGetSlotEntity(uid, slot, out var itemUid) && _itemSlotManager.OnButtonPressed(args, itemUid.Value))
                return;

            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            // only raise event if either itemUid is not null, or the user is holding something
            if (itemUid != null || TryComp(uid, out SharedHandsComponent? hands) && hands.ActiveHandEntity != null)
                EntityManager.RaisePredictiveEvent(new UseSlotNetworkMessage(slot));
        }

        private ItemSlotButton GenerateButton(EntityUid uid, SlotDefinition definition)
        {
            var btn = new ItemSlotButton(ButtonSize, $"{definition.TextureName}.png", "back.png",
                _gameHud)
            {
                OnStoragePressed = (e) =>
                {
                    if (e.Function != EngineKeyFunctions.UIClick &&
                        e.Function != ContentKeyFunctions.ActivateItemInWorld)
                        return;
                    RaiseNetworkEvent(new OpenSlotStorageNetworkMessage(definition.Name));
                }
            };
            btn.OnHover = (_) =>
            {
                HoverInSlotButton(uid, definition.Name, btn);
            };
            btn.OnPressed = (e) =>
            {
                HandleSlotButtonPressed(uid, definition.Name, btn, e);
            };
            return btn;
        }

        private bool TryGetUIElements(EntityUid uid, [NotNullWhen(true)] out DefaultWindow? invWindow,
            [NotNullWhen(true)] out Control? invBottomLeft, [NotNullWhen(true)] out Control? invBottomRight,
            [NotNullWhen(true)] out Control? invTopQuick, ClientInventoryComponent? component = null)
        {
            invWindow = null;
            invBottomLeft = null;
            invBottomRight = null;
            invTopQuick = null;

            if (!Resolve(uid, ref component))
                return false;

            if(!_prototypeManager.TryIndex<InventoryTemplatePrototype>(component.TemplateId, out var template))
                return false;

            if (!_uiGenerateDelegates.TryGetValue(component.TemplateId, out var genfunc))
            {
                _uiGenerateDelegates[component.TemplateId] = genfunc = (entityUid, list) =>
                {
                    var window = new DefaultWindow()
                    {
                        Title = Loc.GetString("human-inventory-window-title"),
                        Resizable = false
                    };
                    window.OnClose += () =>
                    {
                        _gameHud.InventoryButtonDown = false;
                        _gameHud.TopInventoryQuickButtonContainer.Visible = false;
                    };
                    var windowContents = new LayoutContainer
                    {
                        MinSize = (ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation,
                            ButtonSize * 4 + ButtonSeparation * 3)
                    };
                    window.Contents.AddChild(windowContents);

                    var topQuick = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 5
                    };
                    var bottomRight = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 5
                    };
                    var bottomLeft = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 5
                    };

                    foreach (var slotDefinition in template.Slots)
                    {
                        var hudButton = GenerateButton(entityUid, slotDefinition);
                        switch (slotDefinition.UIContainer)
                        {
                            case SlotUIContainer.BottomLeft:
                                bottomLeft.AddChild(hudButton);
                                break;
                            case SlotUIContainer.BottomRight:
                                bottomRight.AddChild(hudButton);
                                break;
                            case SlotUIContainer.Top:
                                topQuick.AddChild(hudButton);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        var windowButton = GenerateButton(entityUid, slotDefinition);
                        list[slotDefinition.Name] = (hudButton, windowButton);
                        windowContents.AddChild(windowButton);
                        LayoutContainer.SetPosition(windowButton, slotDefinition.UIWindowPosition * (ButtonSize + ButtonSeparation));
                    }

                    return (window, bottomLeft, bottomRight, topQuick);
                };
            }

            var res = genfunc(uid, component.SlotButtons);
            invWindow = res.window;
            invBottomLeft = res.bottomLeft;
            invBottomRight = res.bottomRight;
            invTopQuick = res.topQuick;
            return true;
        }


        private void HandleOpenInventoryMenu()
        {
            _gameHud.InventoryButtonDown = !_gameHud.InventoryButtonDown;
            _gameHud.TopInventoryQuickButtonContainer.Visible = _gameHud.InventoryButtonDown;
        }
    }
}

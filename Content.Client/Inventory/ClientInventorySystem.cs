using System;
using System.Collections.Generic;
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
using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : InventorySystem
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
            Dictionary<string, Func<EntityUid, Dictionary<string, List<ItemSlotButton>>, (SS14Window window, Control bottomLeft, Control bottomRight, Control
                topQuick)>>
            _uiGenerateDelegates = new();

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

            SubscribeLocalEvent<ClientInventoryComponent, DidEquipEvent>(OnDidEquip);
            SubscribeLocalEvent<ClientInventoryComponent, DidUnequipEvent>(OnDidUnequip);

            _config.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);
        }

        public override bool TryEquip(EntityUid actor, EntityUid target, EntityUid itemUid, string slot, bool silent = false, bool force = false,
            InventoryComponent? inventory = null, SharedItemComponent? item = null)
        {
            if(!target.IsClientSide() && !actor.IsClientSide() && !itemUid.IsClientSide()) RaiseNetworkEvent(new TryEquipNetworkMessage(actor, target, itemUid, slot, silent, force));
            return base.TryEquip(actor, target, itemUid, slot, silent, force, inventory, item);
        }

        public override bool TryUnequip(EntityUid actor, EntityUid target, string slot, [NotNullWhen(true)] out EntityUid? removedItem, bool silent = false, bool force = false,
            InventoryComponent? inventory = null)
        {
            if(!target.IsClientSide() && !actor.IsClientSide()) RaiseNetworkEvent(new TryUnequipNetworkMessage(actor, target, slot, silent, force));
            return base.TryUnequip(actor, target, slot, out removedItem, silent, force, inventory);
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

            if (!component.SlotButtons.TryGetValue(slot, out var buttons))
                return;

            UpdateUISlot(buttons, item);
        }

        private void UpdateUISlot(List<ItemSlotButton> buttons, EntityUid? entity)
        {
            foreach (var button in buttons)
            {
                _itemSlotManager.SetItemSlot(button, entity);
            }
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
                foreach (var slotButton in inventoryComponent.SlotButtons)
                {
                    foreach (var btn in slotButton.Value)
                    {
                        btn.RefreshTextures(_gameHud);
                    }
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
                foreach (var (slot, buttons) in component.SlotButtons)
                {
                    if (!TryGetSlotEntity(uid, slot, out var entity, component, containerManager))
                        continue;

                    UpdateUISlot(buttons, entity);
                }
            }

            component.InventoryWindow = window;
            component.BottomLeftButtons = bottomLeft;
            component.BottomRightButtons = bottomRight;
            component.TopQuickButtons = topQuick;
        }

        private void HoverInSlotButton(EntityUid uid, string slot, ItemSlotButton button, InventoryComponent? inventoryComponent = null, SharedHandsComponent? hands = null)
        {
            if (!Resolve(uid, ref inventoryComponent))
                return;

            if (!Resolve(uid, ref hands, false))
                return;

            if (!hands.TryGetActiveHeldEntity(out var heldEntity))
                return;

            if(!TryGetSlotContainer(uid, slot, out var containerSlot, out var slotDef, inventoryComponent))
                return;

            _itemSlotManager.HoverInSlot(button, heldEntity.Value,
                CanEquip(uid, heldEntity.Value, slot, out _, slotDef, inventoryComponent) &&
                containerSlot.CanInsert(heldEntity.Value, EntityManager));
        }

        private void HandleSlotButtonPressed(EntityUid uid, string slot, ItemSlotButton button,
            GUIBoundKeyEventArgs args)
        {
            if (TryGetSlotEntity(uid, slot, out var itemUid))
            {
                if (!_itemSlotManager.OnButtonPressed(args, itemUid.Value) && args.Function == EngineKeyFunctions.UIClick)
                {
                    RaiseNetworkEvent(new UseSlotNetworkMessage(uid, slot));
                }
                return;
            }

            if (args.Function != EngineKeyFunctions.UIClick) return;
            TryEquipActiveHandTo(uid, slot);
        }

        private bool TryGetUIElements(EntityUid uid, [NotNullWhen(true)] out SS14Window? invWindow,
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
                    var window = new SS14Window()
                    {
                        Title = Loc.GetString("human-inventory-window-title"),
                        Resizable = false
                    };
                    window.OnClose += () => _gameHud.InventoryButtonDown = false;
                    var windowContents = new LayoutContainer
                    {
                        MinSize = (ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation,
                            ButtonSize * 4 + ButtonSeparation * 3)
                    };
                    window.Contents.AddChild(windowContents);

                    ItemSlotButton GetButton(SlotDefinition definition, string textureBack)
                    {
                        var btn = new ItemSlotButton(ButtonSize, $"{definition.TextureName}.png", textureBack,
                            _gameHud)
                        {
                            OnStoragePressed = (e) =>
                            {
                                if (e.Function != EngineKeyFunctions.UIClick &&
                                    e.Function != ContentKeyFunctions.ActivateItemInWorld)
                                    return;
                                RaiseNetworkEvent(new OpenSlotStorageNetworkMessage(entityUid, definition.Name));
                            }
                        };
                        btn.OnHover = (_) =>
                        {
                            HoverInSlotButton(entityUid, definition.Name, btn);
                        };
                        btn.OnPressed = (e) =>
                        {
                            HandleSlotButtonPressed(entityUid, definition.Name, btn, e);
                        };
                        return btn;
                    }

                    void AddButton(SlotDefinition definition, Vector2i position)
                    {
                        var button = GetButton(definition, "back.png");
                        LayoutContainer.SetPosition(button, position);
                        windowContents.AddChild(button);
                        if (!list.ContainsKey(definition.Name))
                            list[definition.Name] = new();
                        list[definition.Name].Add(button);
                    }

                    void AddHUDButton(BoxContainer container, SlotDefinition definition)
                    {
                        var button = GetButton(definition, "back.png");
                        container.AddChild(button);
                        if (!list.ContainsKey(definition.Name))
                            list[definition.Name] = new();
                        list[definition.Name].Add(button);
                    }

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

                    const int sizep = (ButtonSize + ButtonSeparation);

                    foreach (var slotDefinition in template.Slots)
                    {
                        switch (slotDefinition.UIContainer)
                        {
                            case SlotUIContainer.BottomLeft:
                                AddHUDButton(bottomLeft, slotDefinition);
                                break;
                            case SlotUIContainer.BottomRight:
                                AddHUDButton(bottomRight, slotDefinition);
                                break;
                            case SlotUIContainer.Top:
                                AddHUDButton(topQuick, slotDefinition);
                                break;
                        }

                        AddButton(slotDefinition, slotDefinition.UIWindowPosition * sizep);
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
        }
    }
}

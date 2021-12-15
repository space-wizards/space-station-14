using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.Clothing;
using Content.Client.HUD;
using Content.Shared.Input;
using Content.Client.Items.Components;
using Content.Client.Items.UI;
using Content.Shared.CCVar;
using Content.Shared.CharacterAppearance;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
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

        public const int ButtonSize = 64;
        private const int ButtonSeparation = 4;
        private const int RightSeparation = 2;

        /// <summary>
        /// Stores delegates used to create controls for a given <see cref="InventoryTemplatePrototype"/>.
        /// </summary>
        private readonly
            Dictionary<string, Func<EntityUid, List<ItemSlotButton>, (SS14Window window, Control bottomLeft, Control bottomRight, Control
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

            _config.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);
        }

        private void OnPlayerDetached(EntityUid uid, ClientInventoryComponent component, PlayerDetachedEvent? args = null)
        {
            _gameHud.InventoryButtonVisible = false;
            if (component.BottomLeftButtons != null)
                _gameHud.BottomLeftInventoryQuickButtonContainer.RemoveChild(component.BottomLeftButtons);
            if (component.BottomRightButtons != null)
                _gameHud.BottomRightInventoryQuickButtonContainer.RemoveChild(component.BottomRightButtons);
            if (component.TopQuickButtons != null)
                _gameHud.TopInventoryQuickButtonContainer.RemoveChild(component.TopQuickButtons);
        }

        private void OnShutdown(EntityUid uid, ClientInventoryComponent component, ComponentShutdown args)
        {
            if (_gameHud.InventoryButtonVisible)
                OnPlayerDetached(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, ClientInventoryComponent component, PlayerAttachedEvent args)
        {
            _gameHud.InventoryButtonVisible = true;
            if (component.BottomLeftButtons != null)
                _gameHud.BottomLeftInventoryQuickButtonContainer.AddChild(component.BottomLeftButtons);
            if (component.BottomRightButtons != null)
                _gameHud.BottomRightInventoryQuickButtonContainer.AddChild(component.BottomRightButtons);
            if (component.TopQuickButtons != null)
                _gameHud.TopInventoryQuickButtonContainer.AddChild(component.TopQuickButtons);
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
                    slotButton.RefreshTextures(_gameHud);
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
            if (!TryGetUIElements(uid, out var window, out var bottomLeft, out var bottomRight, out var topQuick,
                    component))
                return;

            component.InventoryWindow = window;
            component.BottomLeftButtons = bottomLeft;
            component.BottomRightButtons = bottomRight;
            component.TopQuickButtons = topQuick;
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
                genfunc = (entityUid, list) =>
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
                        return new ItemSlotButton(ButtonSize, $"{definition.TextureName}.png", textureBack,
                            _gameHud)
                        {
                            OnPressed = (e) =>
                            {
                                if(e.Function != EngineKeyFunctions.UIClick) return;
                                TryEquipActiveHandTo(entityUid, definition.Name);
                            },
                            OnStoragePressed = (e) =>
                            {
                                if (e.Function != EngineKeyFunctions.UIClick &&
                                    e.Function != ContentKeyFunctions.ActivateItemInWorld)
                                    return;
                                //todo paul open storagewindow
                                //ServerStorageComponent.OpenStorageUI
                            },
                            OnHover = (_) =>
                            {
                                //todo paul hover
                            }
                        };
                    }

                    void AddButton(SlotDefinition definition, Vector2i position)
                    {
                        var button = GetButton(definition, "back.png");
                        LayoutContainer.SetPosition(button, position);
                        windowContents.AddChild(button);
                        list.Add(button);
                    }

                    void AddHUDButton(BoxContainer container, SlotDefinition definition)
                    {
                        var button = GetButton(definition, "back.png");
                        container.AddChild(button);
                        list.Add(button);
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.HUD;
using Content.Shared.Input;
using Content.Client.Items.Components;
using Content.Client.Items.UI;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : EntitySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;

        private const int ButtonSize = 64;
        private const int ButtonSeparation = 4;
        private const int RightSeparation = 2;

        /// <summary>
        /// Stores delegates used to create controls for a given <see cref="InventoryTemplatePrototype"/>.
        /// </summary>
        private readonly
            Dictionary<string, Func<(SS14Window window, Control bottomLeft, Control bottomRight, Control topQuick)>>
            _uiGenerateDelegates = new();

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();

            SubscribeLocalEvent<ClientInventoryComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<ClientInventoryComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());

            SubscribeLocalEvent<ClientInventoryComponent, ComponentInit>(OnInit);

            _config.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);
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
            if(!TryGetUIElements(component.TemplateId, out var window, out var bottomLeft, out var bottomRight, out var topQuick))
                return;

            component.InventoryWindow = window;
            component.BottomLeftButtons = bottomLeft;
            component.BottomRightButtons = bottomRight;
            component.TopQuickButtons = topQuick;
        }

        private bool TryGetUIElements(string templateId, [NotNullWhen(true)] out SS14Window? invWindow,
            [NotNullWhen(true)] out Control? invBottomLeft, [NotNullWhen(true)] out Control? invBottomRight,
            [NotNullWhen(true)] out Control? invTopQuick)
        {
            invWindow = null;
            invBottomLeft = null;
            invBottomRight = null;
            invTopQuick = null;
            if(!_prototypeManager.TryIndex<InventoryTemplatePrototype>(templateId, out var template))
                return false;

            if (!_uiGenerateDelegates.TryGetValue(templateId, out var genfunc))
            {
                genfunc = () =>
                {
                    var window = new SS14Window()
                    {
                        Title = Loc.GetString("human-inventory-window-title"),
                        Resizable = false
                    };
                    var windowContents = new LayoutContainer
                    {
                        MinSize = (ButtonSize * 4 + ButtonSeparation * 3 + RightSeparation,
                            ButtonSize * 4 + ButtonSeparation * 3)
                    };
                    window.Contents.AddChild(windowContents);

                    void AddButton(string textureName, Vector2i position)
                    {
                        var button = new ItemSlotButton(ButtonSize, $"{textureName}.png", "back.png", _gameHud);
                        LayoutContainer.SetPosition(button, position);
                        windowContents.AddChild(button);
                    }

                    void AddHUDButton(BoxContainer container, SlotDefinition definition)
                    {
                        var button = new ItemSlotButton(ButtonSize, $"{definition.TextureName}.png", "back.png",
                            _gameHud)
                        {
                            /*OnPressed = (e) => AddToInventory(e, slot),
                            OnStoragePressed = (e) => OpenStorage(e, slot),
                            OnHover = (_) => RequestItemHover(slot)*/
                        };
                        container.AddChild(button);
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

                        AddButton(slotDefinition.TextureName, slotDefinition.UIWindowPosition * sizep);
                    }

                    return (window, bottomLeft, bottomRight, topQuick);
                };
            }

            var res = genfunc();
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

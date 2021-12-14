using System;
using System.Collections.Generic;
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

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();

            SubscribeLocalEvent<ClientInventoryComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<ClientInventoryComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());

            SubscribeLocalEvent<ClientInventoryComponent, SlipAttemptEvent>(OnSlipAttemptEvent);
            SubscribeLocalEvent<ClientInventoryComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
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
            if(!_prototypeManager.TryIndex<InventoryTemplatePrototype>(component.TemplateId, out var template))
                return;

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

            const int sizep = (ButtonSize + ButtonSeparation);

            //todo only generate for local player
            var bottomLeftSet = new HashSet<SlotDefinition>();
            var bottomRightSet = new HashSet<SlotDefinition>();
            var topQuickSet = new HashSet<SlotDefinition>();
            foreach (var slotDefinition in template.Slots)
            {
                switch (slotDefinition.UIContainer)
                {
                    case SlotUIContainer.BottomLeft:
                        bottomLeftSet.Add(slotDefinition);
                        break;
                    case SlotUIContainer.BottomRight:
                        bottomRightSet.Add(slotDefinition);
                        break;
                    case SlotUIContainer.TopQuick:
                        topQuickSet.Add(slotDefinition);
                        break;
                }
                AddButton(slotDefinition.TextureName, slotDefinition.UIWindowPosition*sizep);
            }


        }

        // jesus christ, this is duplicated to server/client, should really just be shared..
        private void OnSlipAttemptEvent(EntityUid uid, ClientInventoryComponent component, SlipAttemptEvent args)
        {
            if (component.TryGetSlot(EquipmentSlotDefines.Slots.SHOES, out EntityUid shoes))
            {
                RaiseLocalEvent(shoes, args, false);
            }
        }

        private void OnRefreshMovespeed(EntityUid uid, ClientInventoryComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            foreach (var (_, ent) in component.AllSlots)
            {
                if (ent != default)
                {
                    RaiseLocalEvent(ent, args, false);
                }
            }
        }

        private void HandleOpenInventoryMenu()
        {
            _gameHud.InventoryButtonDown = !_gameHud.InventoryButtonDown;
        }
    }
}

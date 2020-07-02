using System;
using System.Collections.Generic;
using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Items;
using Content.Client.Utility;
using Content.Shared.Input;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using static Content.Client.StaticIoC;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IItemSlotManager _itemSlotManager;
#pragma warning restore 0649

        private readonly Dictionary<string, ItemSlotButton> _buttons = new Dictionary<string, ItemSlotButton>();
        private readonly Dictionary<string, ItemStatusPanel> _panels = new Dictionary<string, ItemStatusPanel>();

        private readonly TextureRect _activeHandRect;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            var textureHandActive = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_active.png");

            var hands = new HBoxContainer
            {
                SeparationOverride = 0,
            };

            AddChild(hands);

            // Active hand
            _activeHandRect = new TextureRect
            {
                Texture = textureHandActive,
                TextureScale = (2, 2)
            };
        }
        private HBoxContainer GetHandsContainer()
        {
            return (HBoxContainer) GetChild(0);
        }

        private void AddButton(string key, IEntity hand)
        {
            // TODO
            var textureHandLeft = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_l.png");
            var textureHandRight = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_r.png");

            var storageTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/back.png");
            var button = new ItemSlotButton(textureHandLeft, storageTexture);

            button.OnPressed += args => HandKeyBindDown(args, key);
            button.OnStoragePressed += args => _OnStoragePressed(args, key);

            var hBox = GetHandsContainer();

            var texture = ResC.GetTexture("/Nano/item_status_right.svg.96dpi.png");
            var panel = new ItemStatusPanel(texture, StyleBox.Margin.None);

            hBox.AddChild(button);
            hBox.AddChild(panel);

            _buttons[key] = button;
            _panels[key] = panel;

            if (_buttons.Count == 1)
            {
                button.AddChild(_activeHandRect);
                _activeHandRect.SetPositionInParent(1);
            }

            _itemSlotManager.SetItemSlot(button, hand);
        }

        // TODO: Call when hands are removed
        private void RemoveButton(string slot)
        {
            if (!_buttons.Remove(slot, out var button))
            {
                throw new InvalidOperationException($"Slot {slot} has no button");
            }

            if (!_panels.Remove(slot, out var panel))
            {
                throw new InvalidOperationException($"Slot {slot} has no panel");
            }

            if (button.Children.Contains(_activeHandRect))
            {
                button.RemoveChild(_activeHandRect);
            }

            var hBox = GetHandsContainer();
            hBox.RemoveChild(button);
            hBox.RemoveChild(panel);
        }

        /// <summary>
        /// Gets the hands component controlling this gui, returns true if successful and false if failure
        /// </summary>
        /// <param name="hands"></param>
        /// <returns></returns>
        private bool TryGetHands(out HandsComponent hands)
        {
            hands = default;

            var entity = _playerManager?.LocalPlayer?.ControlledEntity;
            return entity != null && entity.TryGetComponent(out hands);
        }

        public void UpdateHandIcons()
        {
            if (Parent == null)
            {
                return;
            }

            UpdateDraw();

            if (!TryGetHands(out var component))
            {
                return;
            }

            foreach (var pair in _buttons)
            {
                var name = pair.Key;

                if (!component.Hands.ContainsKey(name))
                {
                    RemoveButton(name);
                }
            }

            foreach (var pair in component.Hands)
            {
                var name = pair.Key;

                if (!_buttons.ContainsKey(name))
                {
                    AddButton(name, pair.Value);
                }
            }

            _activeHandRect.Parent?.RemoveChild(_activeHandRect);
            var parent = _buttons[component.ActiveIndex];
            parent.AddChild(_activeHandRect);
            _activeHandRect.SetPositionInParent(1);
        }

        private void HandKeyBindDown(GUIBoundKeyEventArgs args, string slotName)
        {
            if (!TryGetHands(out var hands))
            {
                return;
            }

            if (args.Function == ContentKeyFunctions.MouseMiddle)
            {
                hands.SendChangeHand(slotName);
                args.Handle();
                return;
            }

            var entity = hands.GetEntity(slotName);
            if (entity == null)
            {
                if (args.Function == EngineKeyFunctions.UIClick && hands.ActiveIndex != slotName)
                {
                    hands.SendChangeHand(slotName);
                    args.Handle();
                }

                return;
            }

            if (_itemSlotManager.OnButtonPressed(args, entity))
            {
                args.Handle();
                return;
            }

            if (args.Function == EngineKeyFunctions.UIClick)
            {
                if (hands.ActiveIndex == slotName)
                {
                    hands.UseActiveHand();
                }
                else
                {
                    hands.AttackByInHand(slotName);
                }

                args.Handle();
            }
        }

        private void _OnStoragePressed(GUIBoundKeyEventArgs args, string handIndex)
        {
            if (args.Function != EngineKeyFunctions.UIClick || !TryGetHands(out var hands))
            {
                return;
            }

            hands.ActivateItemInHand(handIndex);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!TryGetHands(out var component))
            {
                return;
            }

            foreach (var pair in _buttons)
            {
                var hand = component.Hands[pair.Key];
                var button = pair.Value;

                _itemSlotManager.UpdateCooldown(button, hand);
            }

            foreach (var pair in _panels)
            {
                var hand = component.Hands[pair.Key];
                var panel = pair.Value;

                panel.Update(hand);
            }
        }
    }
}

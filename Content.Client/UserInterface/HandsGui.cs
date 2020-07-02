using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Items;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
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

        private readonly Dictionary<string, HandButton> _buttons = new Dictionary<string, HandButton>();
        private readonly Dictionary<string, ItemStatusPanel> _panels = new Dictionary<string, ItemStatusPanel>();

        private readonly TextureRect _activeHandRect;

        private readonly Texture _leftHandTexture;
        private readonly Texture _middleHandTexture;
        private readonly Texture _rightHandTexture;

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

            _leftHandTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_l.png");
            _middleHandTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_middle.png");
            _rightHandTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_r.png");
        }

        private HBoxContainer GetHandsContainer()
        {
            return (HBoxContainer) GetChild(0);
        }

        private Texture LocationTexture(HandLocation location)
        {
            switch (location)
            {
                case HandLocation.Left:
                    return _leftHandTexture;
                case HandLocation.Middle:
                    return _middleHandTexture;
                case HandLocation.Right:
                    return _rightHandTexture;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }
        }

        /// <summary>
        ///     Adds a new hand button to this control
        /// </summary>
        /// <param name="hand">The hand to add a control for</param>
        /// <param name="buttonLocation">
        ///     The actual location of the button. The right hand is drawn
        ///     on the LEFT of the screen.
        /// </param>
        private void AddButton(SharedHand hand, HandLocation buttonLocation)
        {
            var buttonTexture = LocationTexture(buttonLocation);
            var storageTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/back.png");
            var button = new HandButton(buttonTexture, storageTexture, buttonLocation);
            var slot = hand.Name;

            button.OnPressed += args => HandKeyBindDown(args, slot);
            button.OnStoragePressed += args => _OnStoragePressed(args, slot);

            var hBox = GetHandsContainer();

            var panelTexture = ResC.GetTexture("/Nano/item_status_right.svg.96dpi.png");
            // var panel = new ItemStatusPanel(texture, StyleBox.Margin.None);

            hBox.AddChild(button);

            // hBox.AddChild(panel);

            _buttons[slot] = button;
            // _panels[slot] = panel; // TODO

            if (_buttons.Count == 1)
            {
                button.AddChild(_activeHandRect);
                _activeHandRect.SetPositionInParent(1);
            }

            _itemSlotManager.SetItemSlot(button, hand.Entity);
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

            var locationsOccupied = new HashSet<HandLocation>();
            foreach (var (slot, hand) in component.Hands)
            {
                var location = locationsOccupied.Contains(hand.Location)
                    ? HandLocation.Middle
                    : hand.Location;

                locationsOccupied.Add(location);

                if (!_buttons.ContainsKey(slot))
                {
                    AddButton(hand, location);
                }
            }

            foreach (var button in _buttons.Values)
            {
                if (button.Location == HandLocation.Left)
                {
                    button.SetPositionLast();
                }
                else if (button.Location == HandLocation.Right)
                {
                    button.SetPositionFirst();
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

                _itemSlotManager.UpdateCooldown(button, hand.Entity);
            }

            foreach (var pair in _panels)
            {
                var hand = component.Hands[pair.Key];
                var panel = pair.Value;

                panel.Update(hand.Entity);
            }
        }
    }
}

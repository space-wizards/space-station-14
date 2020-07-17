using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Items;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
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

        private readonly TextureRect _activeHandRect;

        private readonly Texture _leftHandTexture;
        private readonly Texture _middleHandTexture;
        private readonly Texture _rightHandTexture;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            var textureHandActive = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_active.png");

            var hands = new VBoxContainer();

            var panelTexture = ResC.GetTexture("/Textures/Interface/Nano/item_status_left.svg.96dpi.png");
            var panel = new ItemStatusPanel(panelTexture, StyleBox.Margin.None);
            hands.AddChild(panel);
            hands.AddChild(new HBoxContainer
            {
                SeparationOverride = 0,
            });

            AddChild(hands);

            // Active hand
            _activeHandRect = new TextureRect
            {
                Texture = textureHandActive,
                TextureScale = (2, 2)
            };

            _leftHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            _middleHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_middle.png");
            _rightHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_r.png");
        }

        private Control GetHandsContainer()
        {
            return GetChild(0).GetChild(1);
        }

        private ItemStatusPanel GetItemTooltip(Hand hand)
        {
            return (ItemStatusPanel) GetChild(0).GetChild(0);
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
        ///     Adds a new hand to this control
        /// </summary>
        /// <param name="hand">The hand to add to this control</param>
        /// <param name="buttonLocation">
        ///     The actual location of the button. The right hand is drawn
        ///     on the LEFT of the screen.
        /// </param>
        private void AddHand(Hand hand, HandLocation buttonLocation)
        {
            var buttonTexture = LocationTexture(buttonLocation);
            var storageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
            var button = new HandButton(buttonTexture, storageTexture, buttonLocation);
            var slot = hand.Name;

            button.OnPressed += args => HandKeyBindDown(args, slot);
            button.OnStoragePressed += args => _OnStoragePressed(args, slot);

            var hBox = GetHandsContainer();

            var panelTexture = ResC.GetTexture("/Textures/Interface/Nano/item_status_right.svg.96dpi.png");
            // var panel = new ItemStatusPanel(texture, StyleBox.Margin.None);

            hBox.AddChild(button);

            // hBox.AddChild(panel);

            if (_activeHandRect.Parent == null)
            {
                button.AddChild(_activeHandRect);
                _activeHandRect.SetPositionInParent(1);
            }

            hand.Button = button;
            // hand.Panel = panel; // TODO
        }

        public void RemoveHand(Hand hand)
        {
            var hBox = GetHandsContainer();

            var button = hand.Button;
            if (button != null)
            {
                if (button.Children.Contains(_activeHandRect))
                {
                    button.RemoveChild(_activeHandRect);
                }

                hBox.RemoveChild(button);
            }

            var panel = hand.Panel;
            if (panel != null)
            {
                hBox.RemoveChild(panel);
            }

            if (hand.Location == HandLocation.Middle ||
                !TryGetHands(out var hands))
            {
                return;
            }

            foreach (var handsHand in hands.Hands)
            {
                if (handsHand.Location != HandLocation.Middle)
                {
                    continue;
                }

                handsHand.Location = hand.Location;
                break;
            }
        }

        /// <summary>
        ///     Gets the hands component controlling this gui
        /// </summary>
        /// <param name="hands"></param>
        /// <returns>true if successful and false if failure</returns>
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

            // TODO: Remove button on remove hand
            var locationsOccupied = new HashSet<HandLocation>();
            foreach (var hand in component.Hands)
            {
                var location = locationsOccupied.Contains(hand.Location)
                    ? HandLocation.Middle
                    : hand.Location;

                hand.Location = location;
                locationsOccupied.Add(location);
            }

            var hands = component.Hands.OrderByDescending(x => x.Location).ToArray();
            for (var i = 0; i < hands.Length; i++)
            {
                var hand = hands[i];

                if (hand.Button == null)
                {
                    AddHand(hand, hand.Location);
                }

                hand.Button!.Button.Texture = LocationTexture(hand.Location);
                hand.Button!.SetPositionInParent(i);
                _itemSlotManager.SetItemSlot(hand.Button, hand.Entity);
            }

            _activeHandRect.Parent?.RemoveChild(_activeHandRect);
            component[component.ActiveIndex]?.Button?.AddChild(_activeHandRect);

            if (hands.Length > 0)
            {
                _activeHandRect.SetPositionInParent(1);
            }
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

            foreach (var hand in component.Hands)
            {
                if (hand.Button == null)
                {
                    continue;
                }

                _itemSlotManager.UpdateCooldown(hand.Button, hand.Entity);
                // hand.Panel?.Update(hand.Entity); // TODO: For 2 hands
            }

            var tooltip = GetItemTooltip(null); // TODO: Move inside loop, remove null
            tooltip.Update(component.ActiveHand);
        }
    }
}

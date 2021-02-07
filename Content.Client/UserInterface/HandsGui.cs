using System;
using System.Linq;
using Content.Client.GameObjects.Components.Items;
using Content.Client.UserInterface.Stylesheets;
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

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        private readonly Texture _leftHandTexture;
        private readonly Texture _middleHandTexture;
        private readonly Texture _rightHandTexture;

        private readonly ItemStatusPanel _leftPanel;
        private readonly ItemStatusPanel _topPanel;
        private readonly ItemStatusPanel _rightPanel;

        private readonly HBoxContainer _guiContainer;
        private readonly VBoxContainer _handsColumn;
        private readonly HBoxContainer _handsContainer;

        private int _lastHands;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            AddChild(_guiContainer = new HBoxContainer
            {
                SeparationOverride = 0,
                Children =
                {
                    (_rightPanel = ItemStatusPanel.FromSide(HandLocation.Right)),
                    (_handsColumn = new VBoxContainer
                    {
                        Children =
                        {
                            (_topPanel = ItemStatusPanel.FromSide(HandLocation.Middle)),
                            (_handsContainer = new HBoxContainer())
                        }
                    }),
                    (_leftPanel = ItemStatusPanel.FromSide(HandLocation.Left))
                }
            });
            _leftHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            _middleHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            _rightHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_r.png");
        }

        private ItemStatusPanel GetItemPanel(Hand hand)
        {
            return hand.Location switch
            {
                HandLocation.Left => _rightPanel,
                HandLocation.Middle => _topPanel,
                HandLocation.Right => _leftPanel,
                _ => throw new IndexOutOfRangeException()
            };
        }

        private Texture HandTexture(HandLocation location)
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
            var buttonTexture = HandTexture(buttonLocation);
            var storageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
            var blockedTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");
            var button = new HandButton(buttonTexture, storageTexture, blockedTexture, buttonLocation);
            var slot = hand.Name;

            button.OnPressed += args => HandKeyBindDown(args, slot);
            button.OnStoragePressed += args => _OnStoragePressed(args, slot);

            _handsContainer.AddChild(button);
            hand.Button = button;
        }

        public void RemoveHand(Hand hand)
        {
            var button = hand.Button;

            if (button != null)
            {
                _handsContainer.RemoveChild(button);
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

            var hands = component.Hands.OrderByDescending(x => x.Location).ToArray();
            for (var i = 0; i < hands.Length; i++)
            {
                var hand = hands[i];

                if (hand.Button == null)
                {
                    AddHand(hand, hand.Location);
                }

                hand.Button!.Button.Texture = HandTexture(hand.Location);
                hand.Button!.SetPositionInParent(i);
                _itemSlotManager.SetItemSlot(hand.Button, hand.Entity);

                hand.Button!.SetActiveHand(component.ActiveIndex == hand.Name);
            }

            _leftPanel.SetPositionFirst();
            _rightPanel.SetPositionLast();
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

        private void UpdatePanels()
        {
            if (!TryGetHands(out var component))
            {
                return;
            }

            foreach (var hand in component.Hands)
            {
                _itemSlotManager.UpdateCooldown(hand.Button, hand.Entity);
            }

            switch (component.Hands.Count)
            {
                case var n when n == 0 && _lastHands != 0:
                    _guiContainer.Visible = false;

                    _topPanel.Update(null);
                    _leftPanel.Update(null);
                    _rightPanel.Update(null);

                    break;
                case 1:
                    if (_lastHands != 1)
                    {
                        _guiContainer.Visible = true;

                        _topPanel.Update(null);
                        _topPanel.Visible = false;

                        _leftPanel.Update(null);
                        _leftPanel.Visible = false;

                        _rightPanel.Visible = true;

                        if (!_guiContainer.Children.Contains(_rightPanel))
                        {
                            _rightPanel.AddChild(_rightPanel);
                            _rightPanel.SetPositionFirst();
                        }
                    }

                    _rightPanel.Update(component.Hands[0].Entity);

                    break;
                case 2:
                    if (_lastHands != 2)
                    {
                        _guiContainer.Visible = true;
                        _topPanel.Update(null);
                        _topPanel.Visible = false;

                        _leftPanel.Visible = true;
                        _rightPanel.Visible = true;

                        if (_handsColumn.Children.Contains(_topPanel))
                        {
                            _handsColumn.RemoveChild(_topPanel);
                        }
                    }

                    _leftPanel.Update(component.Hands[0].Entity);
                    _rightPanel.Update(component.Hands[1].Entity);

                    // Order is left, right
                    foreach (var hand in component.Hands)
                    {
                        var tooltip = GetItemPanel(hand);
                        tooltip.Update(hand.Entity);
                    }

                    break;
                case var n when n > 2:
                    if (_lastHands <= 2)
                    {
                        _guiContainer.Visible = true;

                        _topPanel.Visible = true;
                        _leftPanel.Visible = false;
                        _rightPanel.Visible = false;

                        if (!_handsColumn.Children.Contains(_topPanel))
                        {
                            _handsColumn.AddChild(_topPanel);
                            _topPanel.SetPositionFirst();
                        }
                    }

                    _topPanel.Update(component.ActiveHand);
                    _leftPanel.Update(null);
                    _rightPanel.Update(null);

                    break;
            }

            _lastHands = component.Hands.Count;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdatePanels();
        }
    }
}

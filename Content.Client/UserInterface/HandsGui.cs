using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.GameObjects.Components.Items;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
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
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

        private Texture _leftHandTexture;
        private Texture _middleHandTexture;
        private Texture _rightHandTexture;

        private readonly ItemStatusPanel _topPanel;

        private readonly HBoxContainer _guiContainer;
        private readonly VBoxContainer _handsColumn;
        private readonly HBoxContainer _handsContainer;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            _configManager.OnValueChanged(CCVars.HudTheme, UpdateHudTheme, invokeImmediately: true);

            AddChild(_guiContainer = new HBoxContainer
            {
                SeparationOverride = 0,
                HorizontalAlignment = HAlignment.Center,
                Children =
                {
                    (_handsColumn = new VBoxContainer
                    {
                        Children =
                        {
                            (_topPanel = ItemStatusPanel.FromSide(HandLocation.Middle)),
                            (_handsContainer = new HBoxContainer{HorizontalAlignment = HAlignment.Center})
                        }
                    }),
                }
            });
            _leftHandTexture = _gameHud.GetHudTexture("hand_l.png");
            _middleHandTexture = _gameHud.GetHudTexture("hand_l.png");
            _rightHandTexture = _gameHud.GetHudTexture("hand_r.png");
        }

        private void UpdateHudTheme(int idx)
        {
            if (!_gameHud.ValidateHudTheme(idx))
            {
                return;
            }
            _leftHandTexture = _gameHud.GetHudTexture("hand_l.png");
            _middleHandTexture = _gameHud.GetHudTexture("hand_l.png");
            _rightHandTexture = _gameHud.GetHudTexture("hand_r.png");
            UpdateHandIcons();
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
            var textureName = "hand_l.png";
            if(buttonLocation == HandLocation.Right)
            {
                textureName = "hand_r.png";
            }
            var buttonTexture = HandTexture(buttonLocation);
            var storageTexture = _gameHud.GetHudTexture("back.png");
            var blockedTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");
            var button = new HandButton(buttonTexture, storageTexture, textureName, blockedTexture, buttonLocation);
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
        private bool TryGetHands([NotNullWhen(true)] out HandsComponent? hands)
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

            _topPanel.Update(component.GetEntity(component.ActiveIndex));
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdatePanels();
        }
    }
}

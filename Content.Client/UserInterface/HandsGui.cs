using System;
using Content.Client.GameObjects;
using Content.Client.Interfaces;
using Content.Client.Interfaces.GameObjects;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private const string HandNameLeft = "left";
        private const string HandNameRight = "right";

        private const int CooldownLevels = 8;

#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IItemSlotManager _itemSlotManager;
#pragma warning restore 0649

        private readonly Texture[] TexturesCooldownOverlay;

        private IEntity LeftHand;
        private IEntity RightHand;

        private readonly TextureRect ActiveHandRect;

        private readonly ItemSlotButton _leftButton;
        private readonly ItemSlotButton _rightButton;

        private readonly ItemStatusPanel _rightStatusPanel;
        private readonly ItemStatusPanel _leftStatusPanel;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            MouseFilter = MouseFilterMode.Stop;

            var textureHandLeft = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_l.png");
            var textureHandRight = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_r.png");
            var textureHandActive = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_active.png");
            var storageTexture = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/back.png");

            TexturesCooldownOverlay = new Texture[CooldownLevels];
            for (var i = 0; i < CooldownLevels; i++)
            {
                TexturesCooldownOverlay[i] =
                    _resourceCache.GetTexture($"/Textures/UserInterface/Inventory/cooldown-{i}.png");
            }

            _rightStatusPanel = new ItemStatusPanel(true);
            _leftStatusPanel = new ItemStatusPanel(false);

            _leftButton = new ItemSlotButton(textureHandLeft, storageTexture);
            _rightButton = new ItemSlotButton(textureHandRight, storageTexture);
            var hBox = new HBoxContainer
            {
                SeparationOverride = 0,
                Children = {_rightStatusPanel, _rightButton, _leftButton, _leftStatusPanel},
                MouseFilter = MouseFilterMode.Ignore
            };

            AddChild(hBox);

            _leftButton.OnPressed += args => HandKeyBindDown(args.Event, HandNameLeft);
            _rightButton.OnPressed += args => HandKeyBindDown(args.Event, HandNameRight);

            // Active hand
            _leftButton.AddChild(ActiveHandRect = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Texture = textureHandActive,
                TextureScale = (2, 2)
            });
        }

        /// <summary>
        /// Gets the hands component controling this gui, returns true if successful and false if failure
        /// </summary>
        /// <param name="hands"></param>
        /// <returns></returns>
        private bool TryGetHands(out IHandsComponent hands)
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

            if (!TryGetHands(out var hands))
                return;

            var left = hands.GetEntity(HandNameLeft);
            var right = hands.GetEntity(HandNameRight);

            ActiveHandRect.Parent.RemoveChild(ActiveHandRect);
            var parent = hands.ActiveIndex == HandNameLeft ? _leftButton : _rightButton;
            parent.AddChild(ActiveHandRect);
            ActiveHandRect.SetPositionInParent(1);

            if (left != null)
            {
                if (left != LeftHand)
                {
                    LeftHand = left;
                    if (LeftHand.TryGetComponent(out ISpriteComponent sprite))
                    {
                        _leftButton.SpriteView.Sprite = sprite;
                    }
                }
            }
            else
            {
                LeftHand = null;
                _leftButton.SpriteView.Sprite = null;
            }

            if (right != null)
            {
                if (right != RightHand)
                {
                    RightHand = right;
                    if (RightHand.TryGetComponent(out ISpriteComponent sprite))
                    {
                        _rightButton.SpriteView.Sprite = sprite;
                    }
                }
            }
            else
            {
                RightHand = null;
                _rightButton.SpriteView.Sprite = null;
            }
        }

        private void HandKeyBindDown(GUIBoundKeyEventArgs args, string handIndex)
        {
            args.Handle();

            if (!TryGetHands(out var hands))
                return;

            if (args.Function == ContentKeyFunctions.MouseMiddle)
            {
                hands.SendChangeHand(handIndex);
                return;
            }

            var entity = hands.GetEntity(handIndex);
            if (entity == null)
            {
                if (args.CanFocus && hands.ActiveIndex != handIndex)
                {
                    hands.SendChangeHand(handIndex);
                }
                return;
            }

            if (_itemSlotManager.OnButtonPressed(args, entity))
                return;

            if (args.CanFocus)
            {
                if (hands.ActiveIndex == handIndex)
                {
                    hands.UseActiveHand();
                }
                else
                {
                    hands.AttackByInHand(handIndex);
                }
                return;
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            UpdateCooldown(_leftButton.CooldownCircle, LeftHand);
            UpdateCooldown(_rightButton.CooldownCircle, RightHand);

            _rightStatusPanel.Update(RightHand);
            _leftStatusPanel.Update(LeftHand);
        }

        private void UpdateCooldown(TextureRect cooldownTexture, IEntity entity)
        {
            if (entity != null
                && entity.TryGetComponent(out ItemCooldownComponent cooldown)
                && cooldown.CooldownStart.HasValue
                && cooldown.CooldownEnd.HasValue)
            {
                var start = cooldown.CooldownStart.Value;
                var end = cooldown.CooldownEnd.Value;

                var length = (end - start).TotalSeconds;
                var progress = (_gameTiming.CurTime - start).TotalSeconds;
                var ratio = (float) (progress / length);

                var textureIndex = CalculateCooldownLevel(ratio);
                if (textureIndex == CooldownLevels)
                {
                    cooldownTexture.Visible = false;
                }
                else
                {
                    cooldownTexture.Visible = true;
                    cooldownTexture.Texture = TexturesCooldownOverlay[textureIndex];
                }
            }
            else
            {
                cooldownTexture.Visible = false;
            }
        }

        internal static int CalculateCooldownLevel(float cooldownValue)
        {
            var val = cooldownValue.Clamp(0, 1);
            val *= CooldownLevels;
            return (int) Math.Floor(val);
        }
    }
}

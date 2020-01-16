using System;
using Content.Client.GameObjects;
using Content.Client.GameObjects.EntitySystems;
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
using Robust.Shared.Map;
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
#pragma warning restore 0649

        private readonly Texture TextureHandLeft;
        private readonly Texture TextureHandRight;
        private readonly Texture TextureHandActive;
        private readonly Texture[] TexturesCooldownOverlay;

        private IEntity LeftHand;
        private IEntity RightHand;

        private readonly SpriteView LeftSpriteView;
        private readonly SpriteView RightSpriteView;
        private readonly TextureRect ActiveHandRect;

        private readonly TextureRect CooldownCircleLeft;
        private readonly TextureRect CooldownCircleRight;

        private readonly Control _leftContainer;
        private readonly Control _rightContainer;

        private readonly ItemStatusPanel _rightStatusPanel;
        private readonly ItemStatusPanel _leftStatusPanel;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            MouseFilter = MouseFilterMode.Stop;

            TextureHandLeft = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_l.png");
            TextureHandRight = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_r.png");
            TextureHandActive = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_active.png");

            TexturesCooldownOverlay = new Texture[CooldownLevels];
            for (var i = 0; i < CooldownLevels; i++)
            {
                TexturesCooldownOverlay[i] =
                    _resourceCache.GetTexture($"/Textures/UserInterface/Inventory/cooldown-{i}.png");
            }

            _rightStatusPanel = new ItemStatusPanel(true);
            _leftStatusPanel = new ItemStatusPanel(false);

            _leftContainer = new Control {MouseFilter = MouseFilterMode.Ignore};
            _rightContainer = new Control {MouseFilter = MouseFilterMode.Ignore};
            var hBox = new HBoxContainer
            {
                SeparationOverride = 0,
                Children = {_rightStatusPanel, _rightContainer, _leftContainer, _leftStatusPanel},
                MouseFilter = MouseFilterMode.Ignore
            };

            AddChild(hBox);

            var textureLeft = new TextureRect
            {
                Texture = TextureHandLeft,
                TextureScale = (2, 2)
            };
            textureLeft.OnKeyBindDown += args => HandKeyBindDown(args, HandNameLeft);

            _leftContainer.AddChild(textureLeft);

            var textureRight = new TextureRect
            {
                Texture = TextureHandRight,
                TextureScale = (2, 2)
            };
            textureRight.OnKeyBindDown += args => HandKeyBindDown(args, HandNameRight);

            _rightContainer.AddChild(textureRight);

            _leftContainer.AddChild(ActiveHandRect = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Texture = TextureHandActive,
                TextureScale = (2, 2)
            });

            LeftSpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2),
                OverrideDirection = Direction.South
            };
            _leftContainer.AddChild(LeftSpriteView);

            RightSpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2),
                OverrideDirection = Direction.South
            };
            _rightContainer.AddChild(RightSpriteView);

            // Cooldown circles.
            _leftContainer.AddChild(CooldownCircleLeft = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterMode.Ignore,
                Stretch = TextureRect.StretchMode.KeepCentered,
                TextureScale = (2, 2),
                Visible = false,
            });

            _rightContainer.AddChild(CooldownCircleRight = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterMode.Ignore,
                Stretch = TextureRect.StretchMode.KeepCentered,
                TextureScale = (2, 2),
                Visible = false
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
            var parent = hands.ActiveIndex == HandNameLeft ? _leftContainer : _rightContainer;
            parent.AddChild(ActiveHandRect);
            ActiveHandRect.SetPositionInParent(1);

            if (left != null)
            {
                if (left != LeftHand)
                {
                    LeftHand = left;
                    if (LeftHand.TryGetComponent(out ISpriteComponent sprite))
                    {
                        LeftSpriteView.Sprite = sprite;
                    }
                }
            }
            else
            {
                LeftHand = null;
                LeftSpriteView.Sprite = null;
            }

            if (right != null)
            {
                RightHand = right;
                if (RightHand.TryGetComponent(out ISpriteComponent sprite))
                {
                    RightSpriteView.Sprite = sprite;
                }
            }
            else
            {
                RightHand = null;
                RightSpriteView.Sprite = null;
            }
        }

        private void SendSwitchHandTo(string index)
        {
            if (!TryGetHands(out IHandsComponent hands))
                return;

            hands.SendChangeHand(index);
        }

        private void UseActiveHand()
        {
            if (!TryGetHands(out IHandsComponent hands))
                return;

            //Todo: remove hands interface, so weird
            ((HandsComponent) hands).UseActiveHand();
        }

        private void AttackByInHand(string hand)
        {
            if (!TryGetHands(out var hands))
                return;

            hands.AttackByInHand(hand);
        }

        private void HandKeyBindDown(GUIBoundKeyEventArgs args, string handIndex)
        {
            if (args.Function == EngineKeyFunctions.Use)
            {
                if (!TryGetHands(out var hands))
                    return;

                if (hands.ActiveIndex == handIndex)
                {
                    UseActiveHand();
                }
                else
                {
                    AttackByInHand(handIndex);
                }
            }
            else if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                var examine = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ExamineSystem>();
                if (handIndex == HandNameLeft)
                    examine.DoExamine(LeftHand);
                else if (handIndex == HandNameRight)
                    examine.DoExamine(RightHand);
            }
            else if (args.Function == ContentKeyFunctions.MouseMiddle)
            {
                SendSwitchHandTo(handIndex);
            }
            else if (args.Function == ContentKeyFunctions.OpenContextMenu)
            {
                if (!TryGetHands(out var hands))
                {
                    return;
                }

                var entity = hands.GetEntity(handIndex);
                if (entity == null)
                {
                    return;
                }

                var esm = IoCManager.Resolve<IEntitySystemManager>();
                esm.GetEntitySystem<VerbSystem>()
                    .OpenContextMenu(entity, new ScreenCoordinates(args.PointerLocation.Position));
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            UpdateCooldown(CooldownCircleLeft, LeftHand);
            UpdateCooldown(CooldownCircleRight, RightHand);

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

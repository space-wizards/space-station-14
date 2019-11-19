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
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private const int CooldownLevels = 8;
        private const int BoxSpacing = 0;
        private const int BoxSize = 64;

#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _loc;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 0649

        private readonly Texture TextureHandLeft;
        private readonly Texture TextureHandRight;
        private readonly Texture TextureHandActive;
        private readonly Texture[] TexturesCooldownOverlay;

        private IEntity LeftHand;
        private IEntity RightHand;
        private UIBox2i _handL;
        private UIBox2i _handR;

        private readonly SpriteView LeftSpriteView;
        private readonly SpriteView RightSpriteView;
        private readonly TextureRect ActiveHandRect;

        private readonly TextureRect CooldownCircleLeft;
        private readonly TextureRect CooldownCircleRight;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            ToolTip = _loc.GetString("Your hands");

            _handR = new UIBox2i(0, 0, BoxSize, BoxSize);
            _handL = _handR.Translated((BoxSize + BoxSpacing, 0));

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

            AddChild(new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Texture = TextureHandLeft,
                Size = _handL.Size,
                Position = _handL.TopLeft,
                TextureScale = (2, 2)
            });

            AddChild(new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Texture = TextureHandRight,
                Size = _handR.Size,
                Position = _handR.TopLeft,
                TextureScale = (2, 2)
            });

            AddChild(ActiveHandRect = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Texture = TextureHandActive,
                Size = _handL.Size,
                Position = _handL.TopLeft,
                TextureScale = (2, 2)
            });

            LeftSpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2)
            };
            AddChild(LeftSpriteView);
            LeftSpriteView.Size = _handL.Size;
            LeftSpriteView.Position = _handL.TopLeft;

            RightSpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2)
            };
            AddChild(RightSpriteView);
            RightSpriteView.Size = _handR.Size;
            RightSpriteView.Position = _handR.TopLeft;

            // Cooldown circles.
            AddChild(CooldownCircleLeft = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Position = _handL.TopLeft + (8, 8),
                TextureScale = (2, 2),
                Visible = false,

            });

            AddChild(CooldownCircleRight = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                Position = _handR.TopLeft + (8, 8),
                TextureScale = (2, 2),
                Visible = false
            });
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(BoxSize * 2 + BoxSpacing, BoxSize) * UIScale;
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

            var left = hands.GetEntity("left");
            var right = hands.GetEntity("right");

            ActiveHandRect.Position = hands.ActiveIndex == "left" ? _handL.TopLeft : _handR.TopLeft;

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

        protected override bool HasPoint(Vector2 point)
        {
            return _handL.Contains((Vector2i) point) || _handR.Contains((Vector2i) point);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!args.CanFocus)
            {
                return;
            }

            var leftHandContains = _handL.Contains((Vector2i) args.RelativePosition);
            var rightHandContains = _handR.Contains((Vector2i) args.RelativePosition);

            string handIndex;
            if (leftHandContains)
            {
                handIndex = "left";
            }
            else if (rightHandContains)
            {
                handIndex = "right";
            }
            else
            {
                return;
            }

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
                var ratio = (float)(progress / length);

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

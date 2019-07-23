using Content.Client.GameObjects;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Interfaces.GameObjects;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private const int BoxSpacing = 0;
        private const int BoxSize = 64;

#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _loc;
#pragma warning restore 0649

        private Texture TextureHandLeft;
        private Texture TextureHandRight;
        private Texture TextureHandActive;

        private IEntity LeftHand;
        private IEntity RightHand;
        private UIBox2i _handL;
        private UIBox2i _handR;

        private SpriteView LeftSpriteView;
        private SpriteView RightSpriteView;
        private TextureRect ActiveHandRect;

        protected override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            ToolTip = _loc.GetString("Your hands");

            _handR = new UIBox2i(0, 0, BoxSize, BoxSize);
            _handL = _handR.Translated((BoxSize + BoxSpacing, 0));

            MouseFilter = MouseFilterMode.Stop;

            TextureHandLeft = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_l.png");
            TextureHandRight = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_r.png");
            TextureHandActive = _resourceCache.GetTexture("/Textures/UserInterface/Inventory/hand_active.png");

            AddChild(new TextureRect
            {
                Texture = TextureHandLeft,
                Size = _handL.Size,
                Position = _handL.TopLeft,
                TextureScale = (2, 2)
            });

            AddChild(new TextureRect
            {
                Texture = TextureHandRight,
                Size = _handR.Size,
                Position = _handR.TopLeft,
                TextureScale = (2, 2)
            });

            AddChild(ActiveHandRect = new TextureRect
            {
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

        protected override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

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

            if (args.Button == Mouse.Button.Left)
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

            else if (args.Button == Mouse.Button.Middle)
            {
                SendSwitchHandTo(handIndex);
            }

            else if (args.Button == Mouse.Button.Right)
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
                esm.GetEntitySystem<VerbSystem>().OpenContextMenu(entity, new ScreenCoordinates(args.GlobalPosition));
            }
        }
    }
}

using Content.Client.GameObjects;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Interfaces.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private static readonly Color _inactiveColor = new Color(90, 90, 90);

        private const int BOX_SPACING = 1;

        // The boxes are square so that's both width and height.
        private const int BOX_SIZE = 50;

        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private StyleBoxTexture handBox;
        private StyleBoxTexture inactiveHandBox;

        private IEntity LeftHand;
        private IEntity RightHand;
        private UIBox2i _handL;
        private UIBox2i _handR;

        private SpriteView LeftSpriteView;
        private SpriteView RightSpriteView;

        protected override void Initialize()
        {
            base.Initialize();

            var resMgr = IoCManager.Resolve<IResourceCache>();
            var handsBoxTexture = resMgr.GetResource<TextureResource>("/Textures/UserInterface/handsbox.png");
            handBox = new StyleBoxTexture()
            {
                Texture = handsBoxTexture,
            };
            handBox.SetPatchMargin(StyleBox.Margin.All, 6);
            inactiveHandBox = new StyleBoxTexture(handBox)
            {
                Modulate = _inactiveColor,
            };
            SetMarginsPreset(LayoutPreset.CenterBottom);
            SetAnchorPreset(LayoutPreset.CenterBottom);

            _handL = new UIBox2i(0, 0, BOX_SIZE, BOX_SIZE);
            _handR = _handL.Translated(new Vector2i(BOX_SIZE + BOX_SPACING, 0));
            MouseFilter = MouseFilterMode.Stop;

            LeftSpriteView = new SpriteView {MouseFilter = MouseFilterMode.Ignore};
            AddChild(LeftSpriteView);
            LeftSpriteView.Size = _handL.Size;
            LeftSpriteView.Position = _handL.TopLeft;

            RightSpriteView = new SpriteView {MouseFilter = MouseFilterMode.Ignore};
            AddChild(RightSpriteView);
            RightSpriteView.Size = _handR.Size;
            RightSpriteView.Position = _handR.TopLeft;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(BOX_SIZE * 2 + 1, BOX_SIZE) * UIScale;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (!TryGetHands(out IHandsComponent hands))
                return;

            var leftActive = hands.ActiveIndex == "left";

            var handL = new UIBox2(_handL.TopLeft * UIScale, _handL.BottomRight * UIScale);
            var handR = new UIBox2(_handR.TopLeft * UIScale, _handR.BottomRight * UIScale);

            handle.DrawStyleBox(handBox, leftActive ? handL : handR);
            handle.DrawStyleBox(inactiveHandBox, leftActive ? handR : handL);
        }

        /// <summary>
        /// Gets the hands component controling this gui, returns true if successful and false if failure
        /// </summary>
        /// <param name="hands"></param>
        /// <returns></returns>
        private bool TryGetHands(out IHandsComponent hands)
        {
            hands = null;
            if (_playerManager?.LocalPlayer == null)
            {
                return false;
            }

            IEntity entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent(out hands))
            {
                return false;
            }

            return true;
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

        private static ISpriteProxy GetSpriteMirror(IEntity entity)
        {
            if (entity.TryGetComponent(out ISpriteComponent component))
            {
                return component.CreateProxy();
            }

            return null;
        }
    }
}

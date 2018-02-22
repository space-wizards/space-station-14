using Content.Client.Interfaces.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HandsGui : HBoxContainer
    {
        private static readonly Color _inactiveColor = new Color(90, 90, 90);
        private const int BOX_SPACING = 1;
        // The boxes are square so that's both width and height.
        private const int BOX_SIZE = 80;

        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private StyleBoxTexture handBox;
        private StyleBoxTexture inactiveHandBox;

        private UiHandInfo LeftHand;
        private UiHandInfo RightHand;
        private Box2i handL;
        private Box2i handR;

        protected override void Initialize()
        {
            base.Initialize();

            var _resMgr = IoCManager.Resolve<IResourceCache>();
            var handsBoxTexture = _resMgr.GetResource<TextureResource>("Textures/UserInterface/handsbox.png");
            handBox = new StyleBoxTexture()
            {
                Texture = handsBoxTexture,
            };
            handBox.SetMargin(StyleBox.Margin.All, 6);
            inactiveHandBox = new StyleBoxTexture(handBox)
            {
                Modulate = _inactiveColor,
            };

            SetAnchorPreset(LayoutPreset.CenterBottom);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(BOX_SIZE * 2 + 1, BOX_SIZE);
        }

        protected override void Draw(DrawingHandle handle)
        {
            handle.DrawStyleBox(handBox, new Box2(Vector2.Zero, Size));
            /*
            if (_playerManager?.LocalPlayer.ControlledEntity == null)
            {
                return;
            }

            IEntity entity = _playerManager.LocalPlayer.ControlledEntity;
            if (!entity.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var leftActive = hands.ActiveIndex == "left";

            handSlot.Color = Color.White;
            handSlot.SetTransformToRect(leftActive ? handL : handR);
            handSlot.Draw();

            handSlot.Color = _inactiveColor;
            handSlot.SetTransformToRect(leftActive ? handR : handL);
            handSlot.Draw();

            if (LeftHand.Entity != null && LeftHand.HeldSprite != null)
            {
                var bounds = LeftHand.HeldSprite.LocalBounds;
                LeftHand.HeldSprite.SetTransformToRect(
                    Box2i.FromDimensions(handL.Left + (int)(handL.Width / 2f - bounds.Width / 2f),
                                    handL.Top + (int)(handL.Height / 2f - bounds.Height / 2f),
                                    (int)bounds.Width, (int)bounds.Height));
                LeftHand.HeldSprite.Draw();
            }

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
            {
                var bounds = RightHand.HeldSprite.LocalBounds;
                RightHand.HeldSprite.SetTransformToRect(
                    Box2i.FromDimensions(handR.Left + (int)(handR.Width / 2f - bounds.Width / 2f),
                                    handR.Top + (int)(handR.Height / 2f - bounds.Height / 2f),
                                    (int)bounds.Width, (int)bounds.Height));
                RightHand.HeldSprite.Draw();
            }
            */
        }

        public void UpdateHandIcons()
        {
            UpdateDraw();
            if (_playerManager?.LocalPlayer.ControlledEntity == null)
            {
                return;
            }

            IEntity entity = _playerManager.LocalPlayer.ControlledEntity;
            if (!entity.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var left = hands.GetEntity("left");
            var right = hands.GetEntity("right");

            if (left != null)
            {
                if (left != LeftHand.Entity)
                {
                    LeftHand.Entity = left;
                    LeftHand.HeldSprite = GetIconSprite(left);
                }
            }
            else
            {
                LeftHand.Entity = null;
                LeftHand.HeldSprite = null;
            }

            if (right != null)
            {
                if (right != RightHand.Entity)
                {
                    RightHand.Entity = right;
                    RightHand.HeldSprite = GetIconSprite(right);
                }
            }
            else
            {
                RightHand.Entity = null;
                RightHand.HeldSprite = null;
            }
        }

        private void SendSwitchHandTo(string index)
        {
            IEntity entity = _playerManager.LocalPlayer.ControlledEntity;
            if (!entity.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }
            hands.SendChangeHand(index);
        }

        /*
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (e.Button != Mouse.Button.Right)
            {
                return false;
            }
            if (handL.Contains(e.X, e.Y))
            {
                SendSwitchHandTo("left");
                return true;
            }
            if (handR.Contains(e.X, e.Y))
            {
                SendSwitchHandTo("right");
                return true;
            }
            return false;
        }
        */

        private static Texture GetIconSprite(IEntity entity)
        {
            if (entity.TryGetComponent<IconComponent>(out var component) && component.Icon != null)
            {
                return component.Icon;
            }
            return IoCManager.Resolve<IResourceCache>().GetFallback<TextureResource>();
        }

        private struct UiHandInfo
        {
            public IEntity Entity { get; set; }
            public Texture HeldSprite { get; set; }
        }
    }
}

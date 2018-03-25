using Content.Client.Interfaces.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
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
        private const int BOX_SIZE = 50;

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
            SetMarginsPreset(LayoutPreset.CenterBottom);
            SetAnchorPreset(LayoutPreset.CenterBottom);

            handL = new Box2i(0, 0, BOX_SIZE, BOX_SIZE);
            handR = handL.Translated(new Vector2i(BOX_SIZE + BOX_SPACING, 0));
            SS14.Shared.Log.Logger.Debug($"{handL}, {handR}");
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(BOX_SIZE * 2 + 1, BOX_SIZE);
        }

        protected override void Draw(DrawingHandle handle)
        {
            if (_playerManager?.LocalPlayer == null)
            {
                return;
            }

            IEntity entity = _playerManager.LocalPlayer.ControlledEntity;
            if (entity == null || !entity.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var leftActive = hands.ActiveIndex == "left";

            handle.DrawStyleBox(handBox, leftActive ? handL : handR);
            handle.DrawStyleBox(inactiveHandBox, leftActive ? handR : handL);

            if (LeftHand.Entity != null && LeftHand.HeldSprite != null)
            {
                var bounds = LeftHand.HeldSprite.Size;
                handle.DrawTextureRect(LeftHand.HeldSprite,
                    Box2i.FromDimensions(handL.Left + (int)(handL.Width / 2f - bounds.X / 2f),
                                    handL.Top + (int)(handL.Height / 2f - bounds.Y / 2f),
                                    (int)bounds.X, (int)bounds.Y), tile: false);
            }

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
            {
                var bounds = RightHand.HeldSprite.Size;
                handle.DrawTextureRect(RightHand.HeldSprite,
                    Box2i.FromDimensions(handR.Left + (int)(handR.Width / 2f - bounds.Y / 2f),
                                    handR.Top + (int)(handR.Height / 2f - bounds.Y / 2f),
                                    (int)bounds.X, (int)bounds.Y), tile: false);
            }
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

        protected override bool HasPoint(Vector2 point)
        {
            return handL.Contains((Vector2i)point) || handR.Contains((Vector2i)point);
        }

        protected override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            if (args.Button != Mouse.Button.Right)
            {
                return;
            }

            if (handL.Contains((Vector2i)args.RelativePosition))
            {
                SendSwitchHandTo("left");
            }
            if (handR.Contains((Vector2i)args.RelativePosition))
            {
                SendSwitchHandTo("right");
            }
        }

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

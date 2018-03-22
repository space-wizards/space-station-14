using Content.Client.Interfaces.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private readonly Color _inactiveColor = new Color(90, 90, 90);

        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        private readonly Sprite handSlot;
        private readonly int spacing = 1;

        private UiHandInfo LeftHand;
        private UiHandInfo RightHand;
        private Box2i handL;
        private Box2i handR;

        public HandsGui()
        {
            var _resMgr = IoCManager.Resolve<IResourceCache>();
            handSlot = _resMgr.GetSprite("hand");
            // OnCalcRect() calculates position so this needs to be ran
            // as it doesn't automatically get called by the UI manager.
            DoLayout();
        }

        protected override void OnCalcRect()
        {
            // Individual size of the hand slot sprite.
            var slotBounds = handSlot.LocalBounds;
            var width = (int)((slotBounds.Width * 2) + spacing);
            var height = (int)slotBounds.Height;

            // Force size because refactoring is HARD.
            Size = new Vector2i(width, height);
            ClientArea = Box2i.FromDimensions(0, 0, Width, Height);

            // Hell force position too what could go wrong!
            Position = new Vector2i((int)(CluwneLib.Window.Viewport.Width - width) / 2, (int)CluwneLib.Window.Viewport.Height - height - 10);
            handL = Box2i.FromDimensions(Position.X, Position.Y, (int)slotBounds.Width, (int)slotBounds.Height);
            handR = Box2i.FromDimensions(Position.X + (int)slotBounds.Width + spacing, Position.Y, (int)slotBounds.Width, (int)slotBounds.Height);
        }

        protected override void DrawContents()
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
        }

        public void UpdateHandIcons()
        {
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

        private static Sprite GetIconSprite(IEntity entity)
        {
            Sprite icon = null;
            if (entity.TryGetComponent<IconComponent>(out var component))
            {
                icon = component.Icon;
            }
            return icon ?? IoCManager.Resolve<IResourceCache>().DefaultSprite();
        }

        private struct UiHandInfo
        {
            public IEntity Entity { get; set; }
            public Sprite HeldSprite { get; set; }
        }
    }
}

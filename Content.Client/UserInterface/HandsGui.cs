using Content.Client.Interfaces.GameObjects;
using OpenTK.Graphics;
using SFML.Graphics;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HandsGui : GuiComponent
    {
        private readonly Color4 _inactiveColor = new Color4(90, 90, 90, 255);

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
            ComponentClass = GuiComponentType.HandsUi;
            handSlot = _resMgr.GetSprite("hand");
            ZDepth = 5;
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);
            UpdateHandIcons();
        }

        public override void Update(float frameTime)
        {
            var slotBounds = handSlot.GetLocalBounds();
            var width = (int)((slotBounds.Width * 2) + spacing);
            var height = (int)slotBounds.Height;
            Position = new Vector2i((int)(CluwneLib.Window.Viewport.Width - width) / 2, (int)CluwneLib.Window.Viewport.Height - height - 10);
            handL = Box2i.FromDimensions(Position.X, Position.Y, (int)slotBounds.Width, (int)slotBounds.Height);
            handR = Box2i.FromDimensions(Position.X + (int)slotBounds.Width + spacing, Position.Y, (int)slotBounds.Width, (int)slotBounds.Height);
            ClientArea = Box2i.FromDimensions(Position.X, Position.Y, width, (int)slotBounds.Height);
        }

        public override void Render()
        {
            if (_playerManager?.ControlledEntity == null)
            {
                return;
            }

            IEntity entity = _playerManager.ControlledEntity;
            if (!entity.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var leftActive = hands.ActiveIndex == "left";

            handSlot.Color = Color.White;
            handSlot.SetTransformToRect(leftActive ? handL : handR);
            handSlot.Draw();

            handSlot.Color = _inactiveColor.Convert();
            handSlot.SetTransformToRect(leftActive ? handR : handL);
            handSlot.Draw();

            if (LeftHand.Entity != null && LeftHand.HeldSprite != null)
            {
                var bounds = LeftHand.HeldSprite.GetLocalBounds();
                LeftHand.HeldSprite.SetTransformToRect(
                    Box2i.FromDimensions(handL.Left + (int)(handL.Width / 2f - bounds.Width / 2f),
                                  handL.Top + (int)(handL.Height / 2f - bounds.Height / 2f),
                                  (int)bounds.Width, (int)bounds.Height));
                LeftHand.HeldSprite.Draw();
            }

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
            {
                var bounds = RightHand.HeldSprite.GetLocalBounds();
                RightHand.HeldSprite.SetTransformToRect(
                    Box2i.FromDimensions(handR.Left + (int)(handR.Width / 2f - bounds.Width / 2f),
                                  handR.Top + (int)(handR.Height / 2f - bounds.Height / 2f),
                                  (int)bounds.Width, (int)bounds.Height));
                RightHand.HeldSprite.Draw();
            }
        }

        public void UpdateHandIcons()
        {
            if (_playerManager?.ControlledEntity == null)
            {
                return;
            }

            IEntity entity = _playerManager.ControlledEntity;
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
            IEntity entity = _playerManager.ControlledEntity;
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

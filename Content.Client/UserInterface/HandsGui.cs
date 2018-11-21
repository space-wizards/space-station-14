using Content.Client.GameObjects;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Interfaces.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Player;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;

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

        private UiHandInfo LeftHand;
        private UiHandInfo RightHand;
        private UIBox2i handL;
        private UIBox2i handR;

        protected override void Initialize()
        {
            base.Initialize();

            var _resMgr = IoCManager.Resolve<IResourceCache>();
            var handsBoxTexture = _resMgr.GetResource<TextureResource>("/Textures/UserInterface/handsbox.png");
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

            handL = new UIBox2i(0, 0, BOX_SIZE, BOX_SIZE);
            handR = handL.Translated(new Vector2i(BOX_SIZE + BOX_SPACING, 0));
            SS14.Shared.Log.Logger.Debug($"{handL}, {handR}");
            MouseFilter = MouseFilterMode.Stop;
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(BOX_SIZE * 2 + 1, BOX_SIZE);
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (!TryGetHands(out IHandsComponent hands))
                return;

            var leftActive = hands.ActiveIndex == "left";

            handle.DrawStyleBox(handBox, leftActive ? handL : handR);
            handle.DrawStyleBox(inactiveHandBox, leftActive ? handR : handL);

            /*
            if (LeftHand.Entity != null && LeftHand.HeldSprite != null)
            {
                var bounds = LeftHand.HeldSprite.Size;
                handle.DrawTextureRect(LeftHand.HeldSprite,
                    UIBox2i.FromDimensions(handL.Left + (int)(handL.Width / 2f - bounds.X / 2f),
                                    handL.Top + (int)(handL.Height / 2f - bounds.Y / 2f),
                                    (int)bounds.X, (int)bounds.Y), tile: false);
            }

            if (RightHand.Entity != null && RightHand.HeldSprite != null)
            {
                var bounds = RightHand.HeldSprite.Size;
                handle.DrawTextureRect(RightHand.HeldSprite,
                    UIBox2i.FromDimensions(handR.Left + (int)(handR.Width / 2f - bounds.Y / 2f),
                                    handR.Top + (int)(handR.Height / 2f - bounds.Y / 2f),
                                    (int)bounds.X, (int)bounds.Y), tile: false);
            }
            */
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

            if (!TryGetHands(out IHandsComponent hands))
                return;

            var left = hands.GetEntity("left");
            var right = hands.GetEntity("right");

            // I'm naively gonna assume all items are 32x32.
            //const float HalfSize = 16;

            if (left != null)
            {
                if (left != LeftHand.Entity)
                {
                    LeftHand.Entity = left;
                    LeftHand.MirrorHandle?.Dispose();
                    LeftHand.MirrorHandle = GetSpriteMirror(left);
                    LeftHand.MirrorHandle.AttachToControl(this);
                    LeftHand.MirrorHandle.Offset = new Vector2(handL.Left + (int) (handL.Width / 2f),
                        handL.Top + (int) (handL.Height / 2f));
                }
            }
            else
            {
                LeftHand.Entity = null;
                LeftHand.MirrorHandle?.Dispose();
                LeftHand.MirrorHandle = null;
            }

            if (right != null)
            {
                if (right != RightHand.Entity)
                {
                    RightHand.Entity = right;
                    RightHand.MirrorHandle?.Dispose();
                    RightHand.MirrorHandle = GetSpriteMirror(right);
                    RightHand.MirrorHandle.AttachToControl(this);
                    RightHand.MirrorHandle.Offset = new Vector2(handR.Left + (int) (handR.Width / 2f),
                        handR.Top + (int) (handR.Height / 2f));
                }
            }
            else
            {
                RightHand.Entity = null;
                RightHand.MirrorHandle?.Dispose();
                RightHand.MirrorHandle = null;
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
            return handL.Contains((Vector2i) point) || handR.Contains((Vector2i) point);
        }

        protected override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            var leftHandContains = handL.Contains((Vector2i) args.RelativePosition);
            var rightHandContains = handR.Contains((Vector2i) args.RelativePosition);

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

        private struct UiHandInfo
        {
            public IEntity Entity { get; set; }
            public ISpriteProxy MirrorHandle { get; set; }
        }
    }
}

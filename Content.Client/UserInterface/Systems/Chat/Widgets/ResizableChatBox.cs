using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

public sealed class ResizableChatBox : ChatBox
{
        public ResizableChatBox()
        {
            IoCManager.InjectDependencies(this);
        }
// TODO: Revisit the resizing stuff after https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
        // Probably not "supposed" to inject IClyde, but I give up.
        // I can't find any other way to allow this control to properly resize when the
        // window is resized. Resized() isn't reliably called when resizing the window,
        // and layoutcontainer anchor / margin don't seem to adjust how we need
        // them to when the window is resized. We need it to be able to resize
        // within some bounds so that it doesn't overlap other UI elements, while still
        // being freely resizable within those bounds.
        [Dependency] private readonly IClyde _clyde = default!;

        private const int DragMarginSize = 7;
        private const int MinDistanceFromBottom = 255;
        private const int MinLeft = 500;
        private DragMode _currentDrag = DragMode.None;
        private Vector2 _dragOffsetTopLeft;
        private Vector2 _dragOffsetBottomRight;

        private byte _clampIn;

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _clyde.OnWindowResized += ClydeOnOnWindowResized;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _clyde.OnWindowResized -= ClydeOnOnWindowResized;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _currentDrag = GetDragModeFor(args.RelativePosition);

                if (_currentDrag != DragMode.None)
                {
                    _dragOffsetTopLeft = args.PointerLocation.Position / UIScale - Position;
                    _dragOffsetBottomRight = Position + Size - args.PointerLocation.Position / UIScale;
                }
            }

            base.KeyBindDown(args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {

            if (args.Function != EngineKeyFunctions.UIClick)
                return;
            if (_currentDrag != DragMode.None)
            {
                _dragOffsetTopLeft = _dragOffsetBottomRight = Vector2.Zero;
                _currentDrag = DragMode.None;

                // If this is done in MouseDown, Godot won't fire MouseUp as you need focus to receive MouseUps.
                UserInterfaceManager.KeyboardFocused?.ReleaseKeyboardFocus();
            }

            base.KeyBindUp(args);
        }


        // TODO: this drag and drop stuff is somewhat duplicated from Robust BaseWindow but also modified
        [Flags]
        private enum DragMode : byte
        {
            None = 0,
            Bottom = 1 << 1,
            Left = 1 << 2
        }

        private DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            var mode = DragMode.None;

            if (relativeMousePos.Y > Size.Y - DragMarginSize)
            {
                mode = DragMode.Bottom;
            }

            if (relativeMousePos.X < DragMarginSize)
            {
                mode |= DragMode.Left;
            }

            return mode;
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (Parent == null)
                return;

            if (_currentDrag == DragMode.None)
            {
                var cursor = CursorShape.Arrow;
                var previewDragMode = GetDragModeFor(args.RelativePosition);
                switch (previewDragMode)
                {
                    case DragMode.Bottom:
                        cursor = CursorShape.VResize;
                        break;

                    case DragMode.Left:
                        cursor = CursorShape.HResize;
                        break;

                    case DragMode.Bottom | DragMode.Left:
                        cursor = CursorShape.Crosshair;
                        break;
                }

                DefaultCursorShape = cursor;
            }
            else
            {
                var top = Rect.Top;
                var bottom = Rect.Bottom;
                var left = Rect.Left;
                var right = Rect.Right;
                var (minSizeX, minSizeY) = MinSize;
                if ((_currentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + _dragOffsetBottomRight.Y, top + minSizeY);
                }

                if ((_currentDrag & DragMode.Left) == DragMode.Left)
                {
                    var maxX = right - minSizeX;
                    left = Math.Min(args.GlobalPosition.X - _dragOffsetTopLeft.X, maxX);
                }

                ClampSize(left, bottom);
            }
        }

        protected override void UIScaleChanged()
        {
            base.UIScaleChanged();
            ClampAfterDelay();
        }

        private void ClydeOnOnWindowResized(WindowResizedEventArgs obj)
        {
            ClampAfterDelay();
        }

        private void ClampAfterDelay()
        {
            _clampIn = 2;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            // we do the clamping after a delay (after UI scale / window resize)
            // because we need to wait for our parent container to properly resize
            // first, so we can calculate where we should go. If we do it right away,
            // we won't have the correct values from the parent to know how to adjust our margins.
            if (_clampIn <= 0)
                return;

            _clampIn -= 1;
            if (_clampIn == 0)
                ClampSize();
        }

        private void ClampSize(float? desiredLeft = null, float? desiredBottom = null)
        {
            if (Parent == null)
                return;

            // var top = Rect.Top;
            var right = Rect.Right;
            var left = desiredLeft ?? Rect.Left;
            var bottom = desiredBottom ?? Rect.Bottom;

            // clamp so it doesn't go too high or low (leave space for alerts UI)
            var maxBottom = Parent.Size.Y - MinDistanceFromBottom;
            if (maxBottom <= MinHeight)
            {
                // we can't fit in our given space (window made awkwardly small), so give up
                // and overlap at our min height
                bottom = MinHeight;
            }
            else
            {
                bottom = Math.Clamp(bottom, MinHeight, maxBottom);
            }

            var maxLeft = Parent.Size.X - MinWidth;
            if (maxLeft <= MinLeft)
            {
                // window too narrow, give up and overlap at our max left
                left = maxLeft;
            }
            else
            {
                left = Math.Clamp(left, MinLeft, maxLeft);
            }

            LayoutContainer.SetMarginLeft(this, -((right + 10) - left));
            LayoutContainer.SetMarginBottom(this, bottom);
        }

        protected override void MouseExited()
        {
            base.MouseExited();

            if (_currentDrag == DragMode.None)
                DefaultCursorShape = CursorShape.Arrow;
        }
}

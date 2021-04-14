using System;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public sealed class ScalingViewport : Control, IViewportControl
    {
        [Dependency]
        private readonly IClyde _clyde = default!;
        [Dependency]
        private readonly IInputManager _inputManager = default!;

        private IClydeViewport? _viewport;
        private IEye? _eye;
        private Vector2i _viewportSize;
        private int _curRenderScale;
        private ScalingViewportStretchMode _stretchMode = ScalingViewportStretchMode.Bilinear;
        private ScalingViewportRenderScaleMode _renderScaleMode = ScalingViewportRenderScaleMode.One;
        private Vector2 _fixedRenderScale = Vector2.One;

        public IEye? Eye
        {
            get => _eye;
            set
            {
                _eye = value;

                if (_viewport != null)
                    _viewport.Eye = value;
            }
        }

        public Vector2i ViewportSize
        {
            get => _viewportSize;
            set
            {
                _viewportSize = value;
                InvalidateViewport();
            }
        }

        public ScalingViewportStretchMode StretchMode
        {
            get => _stretchMode;
            set
            {
                _stretchMode = value;
                InvalidateViewport();
            }
        }

        public ScalingViewportRenderScaleMode RenderScaleMode
        {
            get => _renderScaleMode;
            set
            {
                _renderScaleMode = value;
                InvalidateViewport();
            }
        }

        public Vector2 FixedRenderScale
        {
            get => _fixedRenderScale;
            set
            {
                _fixedRenderScale = value;
                InvalidateViewport();
            }
        }

        public ScalingViewport()
        {
            IoCManager.InjectDependencies(this);
            RectClipContent = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Handled)
                return;

            _inputManager.ViewportKeyEvent(this, args);
        }


        protected override void FrameUpdate(FrameEventArgs args)
        {
            EnsureViewportCreated();
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            DebugTools.AssertNotNull(_viewport);

            _viewport!.Render();

            _viewport.RenderScreenOverlaysBelow(handle);
            var drawBox = GetDrawBox();
            handle.DrawTextureRect(_viewport.RenderTarget.Texture, drawBox);
            _viewport.RenderScreenOverlaysAbove(handle);
        }

        // Draw box in pixel coords to draw the viewport at.
        private UIBox2 GetDrawBox()
        {
            DebugTools.AssertNotNull(_viewport);

            var vpSize = _viewport!.Size;
            var ourSize = (Vector2) PixelSize;
            var (ratioX, ratioY) = ourSize / vpSize;
            var ratio = Math.Min(ratioX, ratioY);

            var size = vpSize * ratio;
            // Size
            var pos = (ourSize - size) / 2;

            return UIBox2.FromDimensions(pos, size);
        }

        private void RegenerateViewport()
        {
            DebugTools.AssertNull(_viewport);

            var vpSizeBase = ViewportSize;
            var ourSize = PixelSize;
            var (ratioX, ratioY) = ourSize / (Vector2) vpSizeBase;
            var ratio = Math.Min(ratioX, ratioY);
            var renderScale = 1;
            switch (_renderScaleMode)
            {
                case ScalingViewportRenderScaleMode.CeilInt:
                    renderScale = (int) Math.Ceiling(ratio);
                    break;
                case ScalingViewportRenderScaleMode.FloorInt:
                    renderScale = (int) Math.Floor(ratio);
                    break;
            }

            renderScale = Math.Max(1, renderScale);

            _curRenderScale = renderScale;

            _viewport = _clyde.CreateViewport(
                ViewportSize * renderScale,
                new TextureSampleParameters
                {
                    Filter = StretchMode == ScalingViewportStretchMode.Bilinear,
                });

            _viewport.RenderScale = (renderScale, renderScale);

            _viewport.Eye = _eye;
        }

        protected override void Resized()
        {
            base.Resized();

            InvalidateViewport();
        }

        private void InvalidateViewport()
        {
            _viewport?.Dispose();
            _viewport = null;
        }

        public MapCoordinates ScreenToMap(Vector2 coords)
        {
            if (_eye == null)
                return default;

            EnsureViewportCreated();

            var matrix = Matrix3.Invert(LocalToScreenMatrix());

            return _viewport!.LocalToWorld(matrix.Transform(coords));


            /*var relative = coords - GlobalPixelPosition;
            var drawBox = GetDrawBox();

            var relativeToBox = relative - drawBox.TopLeft;
            var scale = drawBox.Size / _viewport!.Size;

            var relativeScaled = relativeToBox / scale;

            return _viewport!.LocalToWorld(relativeScaled);*/
        }

        public Vector2 WorldToScreen(Vector2 map)
        {
            if (_eye == null)
                return default;

            EnsureViewportCreated();

            var vpLocal = _viewport!.WorldToLocal(map);

            var matrix = LocalToScreenMatrix();

            return matrix.Transform(vpLocal);
        }

        private Matrix3 LocalToScreenMatrix()
        {
            DebugTools.AssertNotNull(_viewport);

            var drawBox = GetDrawBox();
            var scaleFactor = drawBox.Size / _viewport!.Size;

            if (scaleFactor == (0, 0))
                // Basically a nonsense scenario, at least make sure to return something that can be inverted.
                return Matrix3.Identity;

            var scale = Matrix3.CreateScale(scaleFactor);
            var translate = Matrix3.CreateTranslation(GlobalPixelPosition + drawBox.TopLeft);

            return scale * translate;
        }

        private void EnsureViewportCreated()
        {
            if (_viewport == null)
            {
                RegenerateViewport();
            }

            DebugTools.AssertNotNull(_viewport);
        }
    }

    public enum ScalingViewportStretchMode
    {
        Bilinear = 0,
        Nearest,
        UpDown,
    }

    public enum ScalingViewportRenderScaleMode
    {
        One = 0,
        FloorInt,
        CeilInt
    }
}

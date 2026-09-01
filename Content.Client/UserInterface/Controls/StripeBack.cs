using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    public sealed class StripeBack : Container
    {
        private const float PadSize = 4;
        private const float EdgeSize = 2;
        private static readonly Color EdgeColor = Color.FromHex("#525252ff");

        private bool _hasTopEdge = true;
        private bool _hasBottomEdge = true;
        private bool _hasMargins = true;

        public const string StylePropertyBackground = "background";

        public bool HasTopEdge
        {
            get => _hasTopEdge;
            set
            {
                _hasTopEdge = value;
                InvalidateMeasure();
            }
        }

        public bool HasBottomEdge
        {
            get => _hasBottomEdge;
            set
            {
                _hasBottomEdge = value;
                InvalidateMeasure();
            }
        }

        public bool HasMargins
        {
            get => _hasMargins;
            set
            {
                _hasMargins = value;
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var padSize = HasMargins ? PadSize : 0;
            var padSizeTotal = 0f;

            if (HasBottomEdge)
                padSizeTotal += padSize + EdgeSize;
            if (HasTopEdge)
                padSizeTotal += padSize + EdgeSize;

            var size = Vector2.Zero;

            availableSize.Y -= padSizeTotal;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                size = Vector2.Max(size, child.DesiredSize);
            }

            return size + new Vector2(0, padSizeTotal);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var padSize = HasMargins ? PadSize : 0;
            var top = HasTopEdge ? padSize + EdgeSize : 0;
            var bottom = HasBottomEdge ? padSize + EdgeSize : 0;

            // Clamp so the box stays valid when finalSize is smaller than the edge insets
            top = MathF.Min(top, finalSize.Y);
            var box = new UIBox2(0, top, finalSize.X, MathF.Max(top, finalSize.Y - bottom));

            foreach (var child in Children)
            {
                child.Arrange(box);
            }

            return finalSize;
        }


        protected override void Draw(DrawingHandleScreen handle)
        {
            var padSize = HasMargins ? PadSize : 0;
            var edgeSize = (padSize + EdgeSize) * UIScale;
            var top = HasTopEdge ? edgeSize : 0;
            var bottom = HasBottomEdge ? edgeSize : 0;

            // Too small to fit the edges, don't draw anything
            if (top + bottom > PixelHeight)
                return;

            var centerBox = new UIBox2(0, top, PixelWidth, PixelHeight - bottom);

            if (HasTopEdge)
            {
                handle.DrawRect(new UIBox2(0, padSize * UIScale, PixelWidth, centerBox.Top), EdgeColor);
            }

            if (HasBottomEdge)
            {
                handle.DrawRect(new UIBox2(0, centerBox.Bottom, PixelWidth, PixelHeight - padSize * UIScale),
                    EdgeColor);
            }

            GetActualStyleBox()?.Draw(handle, centerBox, UIScale);
        }

        private StyleBox? GetActualStyleBox()
        {
            return TryGetStyleProperty(StylePropertyBackground, out StyleBox? box) ? box : null;
        }
    }
}

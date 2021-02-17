using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class StripeBack : Container
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
                MinimumSizeChanged();
                _hasTopEdge = value;
            }
        }

        public bool HasBottomEdge
        {
            get => _hasBottomEdge;
            set
            {
                _hasBottomEdge = value;
                MinimumSizeChanged();
            }
        }

        public bool HasMargins
        {
            get => _hasMargins;
            set
            {
                _hasMargins = value;
                MinimumSizeChanged();
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var size = Vector2.Zero;

            foreach (var child in Children)
            {
                size = Vector2.ComponentMax(size, child.CombinedMinimumSize);
            }

            var padSize = HasMargins ? PadSize : 0;

            if (HasBottomEdge)
            {
                size += (0, padSize + EdgeSize);
            }

            if (HasTopEdge)
            {
                size += (0, padSize + EdgeSize);
            }

            return size;
        }

        protected override void LayoutUpdateOverride()
        {
            var box = SizeBox;

            var padSize = HasMargins ? PadSize : 0;

            if (HasTopEdge)
            {
                box += (0, padSize + EdgeSize, 0, 0);
            }

            if (HasBottomEdge)
            {
                box += (0, 0, 0, -(padSize + EdgeSize));
            }

            foreach (var child in Children)
            {
                FitChildInBox(child, box);
            }
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            UIBox2 centerBox = PixelSizeBox;

            var padSize = HasMargins ? PadSize : 0;

            if (HasTopEdge)
            {
                centerBox += (0, (padSize + EdgeSize) * UIScale, 0, 0);
                handle.DrawRect(new UIBox2(0, padSize * UIScale, PixelWidth, centerBox.Top), EdgeColor);
            }

            if (HasBottomEdge)
            {
                centerBox += (0, 0, 0, -((padSize + EdgeSize) * UIScale));
                handle.DrawRect(new UIBox2(0, centerBox.Bottom, PixelWidth, PixelHeight - padSize * UIScale), EdgeColor);
            }

            GetActualStyleBox()?.Draw(handle, centerBox);
        }

        private StyleBox GetActualStyleBox()
        {
            return TryGetStyleProperty(StylePropertyBackground, out StyleBox box) ? box : null;
        }
    }
}

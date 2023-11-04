using System.Numerics;
using Content.Client.DoAfter;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    public sealed class ProgressTextureRect : TextureRect
    {
        public float Progress;

        protected override void Draw(DrawingHandleScreen handle)
        {
            var dims = Texture != null ? GetDrawDimensions(Texture) : UIBox2.FromDimensions(Vector2.Zero, PixelSize);
            dims.Top = Math.Max(dims.Bottom - dims.Bottom * Progress,0);
            handle.DrawRect(dims, DoAfterOverlay.GetProgressColor(Progress));

            base.Draw(handle);
        }
    }
}

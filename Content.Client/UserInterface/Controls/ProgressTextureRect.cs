using System.Numerics;
using Content.Client.UserInterface.Systems;
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
            handle.DrawRect(dims, ProgressColorSystem.GetProgressColor(Progress));

            base.Draw(handle);
        }
    }
}

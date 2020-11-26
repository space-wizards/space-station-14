using System;
using Content.Client.GameObjects.EntitySystems.DoAfter;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Actor
{
    public class ProgressTextureRect : TextureRect
    {
        public float Progress;

        protected override void Draw(DrawingHandleScreen handle)
        {
            var dims = Texture != null ? GetDrawDimensions(Texture) : UIBox2.FromDimensions(Vector2.Zero, PixelSize);
            dims.Top = Math.Max(dims.Bottom - dims.Bottom * Progress,0);
            handle.DrawRect(dims, DoAfterHelpers.GetProgressColor(Progress));

            base.Draw(handle);
        }
    }
}

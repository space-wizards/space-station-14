using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using System;

namespace Robust.Client.UserInterface.Controls
{
    public class CooldownGraphic : Control
    {
        public float Fraction { get; set; }

        protected override void Draw(DrawingHandleScreen handle)
        {
            const int maxSegments = 64;
            const float segment = MathHelper.TwoPi / maxSegments;

            var segments = (int)Math.Max(2, Math.Ceiling(maxSegments * Fraction)); // ensure that we always have 3 vertices
            var max = MathHelper.TwoPi * Fraction;
            var radius = (Math.Min(SizeBox.Height, SizeBox.Width) / 2) * 0.875f; // 28/32 = 0.875 - 2 pixels inwards from the edge

            Span<Vector2> vertices = stackalloc Vector2[segments + 1];
            vertices[0] = PixelPosition + SizeBox.Center;
            for (int i = 0; i < segments; i++)
            {
                var angle = MathHelper.Pi + Math.Min(max, segment * i);
                vertices[i + 1] = vertices[0] + new Vector2((float) Math.Sin(angle) * radius, (float) Math.Cos(angle) * radius);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, new Color(0.3f, 0.3f, 0.4f, 0.5f));
        }
    }
}

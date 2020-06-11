using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using System;

namespace Robust.Client.UserInterface.Controls
{
    public class CooldownGraphic : Control
    {
        /// <summary>
        ///     Progress of the cooldown animation.
        ///     Possible values range from 1 to -1, where 1 to 0 is a depleting circle animation and 0 to -1 is a blink animation.
        /// </summary>
        public float Progress { get; set; }

        protected override void Draw(DrawingHandleScreen handle)
        {
            const int maxSegments = 64;
            const float segment = MathHelper.TwoPi / maxSegments;

            var effective_fraction = Progress >= 0f ? Progress : 1f; // we want the full circle for the blink

            var segments = (int)Math.Max(2, Math.Ceiling(maxSegments * effective_fraction) + 1); // ensure that we always have at least 3 vertices, we also need one more segment
            var max = MathHelper.TwoPi * effective_fraction;
            var outer_radius = (Math.Min(SizeBox.Height, SizeBox.Width) / 2) * 0.875f; // 28/32 = 0.875 - 2 pixels inwards from the edge
            var inner_radius = (Math.Min(SizeBox.Height, SizeBox.Width) / 2) * 0.5625f; // 18/32 = 0.5625 - 5 pixels thick

            Span <Vector2> vertices = stackalloc Vector2[segments * 2];
            var center = PixelPosition + SizeBox.Center;
            for (int i = 0; i < segments; i++)
            {
                var angle = MathHelper.Pi + Math.Min(max, segment * i);
                vertices[2*i] = center + new Vector2((float) Math.Sin(angle) * outer_radius, (float) Math.Cos(angle) * outer_radius);
                vertices[2*i + 1] = center + new Vector2((float) Math.Sin(angle) * inner_radius, (float) Math.Cos(angle) * inner_radius);
            }

            Color draw_color;

            var fraction_lerp = 1f - Math.Abs(Progress); // for future bikeshedding purposes

            if (Progress >= 0f)
            {
                var hue = (5f / 18f) * fraction_lerp;
                draw_color = Color.FromHsv((hue, 0.75f, 0.75f, 0.50f));
            }
            else
            {
                var alpha = Math.Clamp(0.5f * fraction_lerp, 0f, 0.5f);
                draw_color = new Color(1f, 1f, 1f, alpha);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, vertices, draw_color);
        }
    }
}

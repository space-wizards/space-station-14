using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{

    public class CooldownGraphic : Control
    {

        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        private ShaderInstance _shader;

        public CooldownGraphic()
        {
            IoCManager.InjectDependencies(this);
            _shader = _protoMan.Index<ShaderPrototype>("CooldownAnimation").InstanceUnique();
        }

        /// <summary>
        ///     Progress of the cooldown animation.
        ///     Possible values range from 1 to -1, where 1 to 0 is a depleting circle animation and 0 to -1 is a blink animation.
        /// </summary>
        public float Progress { get; set; }
        private static readonly Color StartColor = new Color(0.8f, 0.0f, 0.2f); // red
        private static readonly Color EndColor = new Color(0.92f, 0.77f, 0.34f); // yellow
        private static readonly Color CompletedColor = new Color(0.0f, 0.8f, 0.27f); // green

        protected override void Draw(DrawingHandleScreen handle)
        {
            Span<float> x = stackalloc float[10];
            Color color;

            var lerp = 1f - MathF.Abs(Progress); // for future bikeshedding purposes

            if (Progress >= 0f)
            {
                color = new Color(
                    EndColor.R + (StartColor.R - EndColor.R) * Progress,
                    EndColor.G + (StartColor.G - EndColor.G) * Progress,
                    EndColor.B + (StartColor.B - EndColor.B) * Progress,
                    EndColor.A);
            }
            else
            {
                var alpha = MathHelper.Clamp(0.5f * lerp, 0f, 0.5f);
                color = CompletedColor.WithAlpha(alpha);
            }

            _shader.SetParameter("progress", Progress);
            handle.UseShader(_shader);
            handle.DrawRect(PixelSizeBox, color);
        }

    }

}

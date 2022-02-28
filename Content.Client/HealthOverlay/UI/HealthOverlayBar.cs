using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.HealthOverlay.UI
{
    public sealed class HealthOverlayBar : Control
    {
        public const byte HealthBarScale = 2;

        private const int XPixelDiff = 20 * HealthBarScale;

        public HealthOverlayBar()
        {
            IoCManager.InjectDependencies(this);
            Shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
        }

        private ShaderInstance Shader { get; }

        /// <summary>
        ///     From -1 (dead) to 0 (crit) and 1 (alive)
        /// </summary>
        public float Ratio { get; set; }

        public Color Color { get; set; }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            handle.UseShader(Shader);

            var leftOffset = 2 * HealthBarScale;
            var box = new UIBox2i(
                leftOffset,
                -2 + 2 * HealthBarScale,
                leftOffset + (int) (XPixelDiff * Ratio * UIScale),
                -2);

            handle.DrawRect(box, Color);
        }
    }
}

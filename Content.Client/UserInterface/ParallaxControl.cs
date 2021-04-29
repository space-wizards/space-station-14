using Content.Client.Interfaces.Parallax;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     Renders the parallax background as a UI control.
    /// </summary>
    public sealed class ParallaxControl : Control
    {
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [ViewVariables(VVAccess.ReadWrite)] public Vector2i Offset { get; set; }

        public ParallaxControl()
        {
            IoCManager.InjectDependencies(this);

            Offset = (_random.Next(0, 1000), _random.Next(0, 1000));
            RectClipContent = true;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var tex = _parallaxManager.ParallaxTexture;
            if (tex == null)
                return;

            var size = tex.Size;
            var ourSize = PixelSize;

            for (var x = -size.X + Offset.X; x < ourSize.X; x += size.X)
            {
                for (var y = -size.Y + Offset.Y; y < ourSize.Y; y += size.Y)
                {
                    handle.DrawTexture(tex, (x, y));
                }
            }
        }
    }
}

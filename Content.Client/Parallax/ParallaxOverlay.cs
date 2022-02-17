using System;
using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax
{
    public sealed class ParallaxOverlay : Overlay
    {
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const float Slowness = 0.5f;

        private Texture? _parallaxTexture;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
        private readonly ShaderInstance _shader;

        public ParallaxOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();

            if (_parallaxManager.ParallaxTexture == null)
            {
                _parallaxManager.OnTextureLoaded += texture => _parallaxTexture = texture;
            }
            else
            {
                _parallaxTexture = _parallaxManager.ParallaxTexture;
            }
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (_parallaxTexture == null || args.Viewport.Eye == null)
            {
                return;
            }

            var screenHandle = args.WorldHandle;
            screenHandle.UseShader(_shader);

            var (sizeX, sizeY) = _parallaxTexture.Size / (float) EyeManager.PixelsPerMeter;
            var (posX, posY) = args.Viewport.Eye.Position;
            var o = new Vector2(posX * Slowness, posY * Slowness);

            // Remove offset so we can floor.
            var (l, b) = args.WorldAABB.BottomLeft - o;

            // Floor to background size.
            l = sizeX * MathF.Floor(l / sizeX);
            b = sizeY * MathF.Floor(b / sizeY);

            // Re-offset.
            l += o.X;
            b += o.Y;

            for (var x = l; x < args.WorldAABB.Right; x += sizeX)
            {
                for (var y = b; y < args.WorldAABB.Top; y += sizeY)
                {
                    screenHandle.DrawTexture(_parallaxTexture, (x, y));
                }
            }
        }
    }
}

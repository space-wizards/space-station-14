using Content.Client.Interfaces.Parallax;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax
{
    public class ParallaxOverlay : Overlay
    {
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override bool AlwaysDirty => true;
        private const float Slowness = 0.5f;

        private Texture _parallaxTexture;

        public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;
        private readonly ShaderInstance _shader;

        public ParallaxOverlay() : base(nameof(ParallaxOverlay))
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

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            if (_parallaxTexture == null)
            {
                return;
            }

            handle.UseShader(_shader);
            var screenHandle = (DrawingHandleScreen) handle;

            var (sizeX, sizeY) = _parallaxTexture.Size;
            var (posX, posY) = _eyeManager.ScreenToMap(Vector2.Zero).Position;
            var (ox, oy) = (Vector2i) new Vector2(-posX / Slowness, posY / Slowness);
            ox = MathHelper.Mod(ox, sizeX);
            oy = MathHelper.Mod(oy, sizeY);

            var (screenSizeX, screenSizeY) = _displayManager.ScreenSize;
            for (var x = -sizeX; x < screenSizeX; x += sizeX) {
                for (var y = -sizeY; y < screenSizeY; y += sizeY) {
                    screenHandle.DrawTexture(_parallaxTexture, new Vector2(ox + x, oy + y));
                }
            }
        }
    }
}

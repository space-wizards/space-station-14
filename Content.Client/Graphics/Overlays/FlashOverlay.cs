using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Graphics.Overlays
{
    public class FlashOverlay : Overlay, IConfigurableOverlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly ShaderInstance _shader;
        private readonly double _startTime;
        private int _lastsFor = 5000;
        private Texture _screenshotTexture;

        public FlashOverlay() : base(nameof(SharedOverlayID.FlashOverlay))
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").Instance().Duplicate();

            _startTime = _gameTiming.CurTime.TotalMilliseconds;
            _displayManager.Screenshot(ScreenshotType.BeforeUI, image =>
            {
                var rgba32Image = image.CloneAs<Rgba32>(Configuration.Default);
                _screenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
            });
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            handle.UseShader(_shader);
            var percentComplete = (float) ((_gameTiming.CurTime.TotalMilliseconds - _startTime) / _lastsFor);
            _shader?.SetParameter("percentComplete", percentComplete);

            var screenSpaceHandle = handle as DrawingHandleScreen;
            var screenSize = UIBox2.FromDimensions((0, 0), _displayManager.ScreenSize);

            if (_screenshotTexture != null)
            {
                screenSpaceHandle?.DrawTextureRect(_screenshotTexture, screenSize);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _screenshotTexture = null;
        }

        public void Configure(OverlayParameter parameters)
        {
            if (parameters is TimedOverlayParameter timedParams)
            {
                _lastsFor = timedParams.Length;
            }
        }
    }
}

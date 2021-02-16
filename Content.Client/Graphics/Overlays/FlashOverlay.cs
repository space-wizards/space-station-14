using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Graphics.Overlays
{
    public class FlashOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly ShaderInstance _shader;
        private double _startTime = -1;
        private double _lastsFor = 1;
        private Texture _screenshotTexture;

        public FlashOverlay() : base(nameof(FlashOverlay))
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").Instance().Duplicate();
        }

        public void ReceiveFlash(double duration)
        {
            _displayManager.Screenshot(ScreenshotType.BeforeUI, image =>
            {
                var rgba32Image = image.CloneAs<Rgba32>(Configuration.Default);
                _screenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
            });
            _startTime = _gameTiming.CurTime.TotalSeconds;
            _lastsFor = duration;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            var percentComplete = (float) ((_gameTiming.CurTime.TotalSeconds - _startTime) / _lastsFor);
            if (percentComplete >= 1.0f)
                return;
            handle.UseShader(_shader);
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
    }
}

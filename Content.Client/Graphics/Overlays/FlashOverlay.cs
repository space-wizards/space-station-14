using Content.Client.State;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Enums;
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
        [Dependency] private readonly IStateManager _stateManager = default!;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private readonly ShaderInstance _shader;
        private double _startTime = -1;
        private double _lastsFor = 1;
        private Texture? _screenshotTexture;

        public FlashOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").Instance().Duplicate();
        }

        public void ReceiveFlash(double duration)
        {
            if (_stateManager.CurrentState is IMainViewportState state)
            {
                state.Viewport.Viewport.Screenshot(image =>
                {
                    var rgba32Image = image.CloneAs<Rgba32>(Configuration.Default);
                    _screenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
                });
            }

            _startTime = _gameTiming.CurTime.TotalSeconds;
            _lastsFor = duration;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var percentComplete = (float) ((_gameTiming.CurTime.TotalSeconds - _startTime) / _lastsFor);
            if (percentComplete >= 1.0f)
                return;

            var screenSpaceHandle = args.ScreenHandle;
            screenSpaceHandle.UseShader(_shader);
            _shader.SetParameter("percentComplete", percentComplete);

            var screenSize = UIBox2.FromDimensions((0, 0), _displayManager.ScreenSize);

            if (_screenshotTexture != null)
            {
                screenSpaceHandle.DrawTextureRect(_screenshotTexture, screenSize);
            }
        }

        protected override void DisposeBehavior()
        {
            base.Dispose();
            _screenshotTexture = null;
        }
    }
}

using System.Numerics;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Flash
{
    public sealed class FlashOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;
        private double _startTime = -1;
        private double _lastsFor = 1;
        private float _percentComplete = 0f;
        private Texture? _screenshotTexture;

        public FlashOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").InstanceUnique();
        }

        public void ReceiveFlash(double duration)
        {
            if (_stateManager.CurrentState is IMainViewportState state)
            {
                state.Viewport.Viewport.Screenshot(image =>
                {
                    var rgba32Image = image.CloneAs<Rgba32>(SixLabors.ImageSharp.Configuration.Default);
                    _screenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
                });
            }

            _startTime = _gameTiming.CurTime.TotalSeconds;
            _lastsFor = duration;
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
                return false;
            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            _percentComplete = (float) ((_gameTiming.CurTime.TotalSeconds - _startTime) / _lastsFor);
            if (_percentComplete >= 1.0f)
            {
                _screenshotTexture = null;
                return false;
            }
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var worldHandle = args.WorldHandle;
            _shader.SetParameter("percentComplete", _percentComplete);
            worldHandle.UseShader(_shader);

            if (_screenshotTexture != null)
            {
                worldHandle.DrawTextureRectRegion(_screenshotTexture, args.WorldBounds);
            }

            worldHandle.UseShader(null);
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();
            _screenshotTexture = null;
        }
    }
}

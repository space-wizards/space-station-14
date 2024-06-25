using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Flash
{
    public sealed class FlashOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

       private readonly StatusEffectsSystem _statusSys;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;
        public float PercentComplete = 0.0f;
        public Texture? ScreenshotTexture;

        public FlashOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").InstanceUnique();
            _statusSys = _entityManager.System<StatusEffectsSystem>();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            var playerEntity = _playerManager.LocalEntity;

            if (playerEntity == null)
                return;

            if (!_entityManager.HasComponent<FlashedComponent>(playerEntity)
                || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
                return;

            if (!_statusSys.TryGetTime(playerEntity.Value, SharedFlashSystem.FlashedKey, out var time, status))
                return;

            var curTime = _timing.CurTime;
            var lastsFor = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
            var timeDone = (float) (curTime - time.Value.Item1).TotalSeconds;

            PercentComplete = timeDone / lastsFor;
        }

        public void ReceiveFlash()
        {
            if (_stateManager.CurrentState is IMainViewportState state)
            {
                // take a screenshot
                // note that the callback takes a while and ScreenshotTexture will be null the first few Draws
                state.Viewport.Viewport.Screenshot(image =>
                {
                    var rgba32Image = image.CloneAs<Rgba32>(SixLabors.ImageSharp.Configuration.Default);
                    ScreenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
                });
            }
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
                return false;
            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            return PercentComplete < 1.0f;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenshotTexture == null)
                return;

            var worldHandle = args.WorldHandle;
            _shader.SetParameter("percentComplete", PercentComplete);
            worldHandle.UseShader(_shader);
            worldHandle.DrawTextureRectRegion(ScreenshotTexture, args.WorldBounds);
            worldHandle.UseShader(null);
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();
            ScreenshotTexture = null;
            PercentComplete = 1.0f;
        }
    }
}

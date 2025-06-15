using Content.Shared.CCVar;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Flash
{
    public sealed class FlashOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private readonly SharedFlashSystem _flash;
        private readonly StatusEffectsSystem _statusSys;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private ShaderInstance _shader;
        public float PercentComplete = 0.0f;
        public Texture? ScreenshotTexture;

        private readonly ProtoId<ShaderPrototype> _flashedEffect = "FlashedEffect";
        private readonly ProtoId<ShaderPrototype> _flashedEffectReducedMotion = "FlashedEffectReducedMotion";

        public FlashOverlay()
        {
            IoCManager.InjectDependencies(this);
            // Default to the normal flashed effect so that _shader can be initialized in the constructor
            // and does not need to be nullable
            _shader = _prototypeManager.Index(_flashedEffect).InstanceUnique();

            // Set the shader according to the CVar when it is changed and also now.
            _configManager.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);

            _flash = _entityManager.System<SharedFlashSystem>();
            _statusSys = _entityManager.System<StatusEffectsSystem>();
        }

        private void OnReducedMotionChanged(bool reducedMotion)
        {
            var effectName = reducedMotion ? _flashedEffectReducedMotion : _flashedEffect;
            _shader = _prototypeManager.Index<ShaderPrototype>(effectName).InstanceUnique();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            var playerEntity = _playerManager.LocalEntity;

            if (playerEntity == null)
                return;

            if (!_entityManager.HasComponent<FlashedComponent>(playerEntity)
                || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
                return;

            if (!_statusSys.TryGetTime(playerEntity.Value, _flash.FlashedKey, out var time, status))
                return;

            var curTime = _timing.CurTime;
            var lastsFor = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
            var timeDone = (float) (curTime - time.Value.Item1).TotalSeconds;

            PercentComplete = timeDone / lastsFor;
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
            if (RequestScreenTexture && ScreenTexture != null)
            {
                ScreenshotTexture = ScreenTexture;
                RequestScreenTexture = false; // we only need the first frame, so we can stop the request now for performance reasons
            }
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
        }
    }
}

using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;

namespace Content.Client.Eye.Blinding
{
    public sealed class BlindOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] IEntityManager _entityManager = default!;
        [Dependency] ILightManager _lightManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _greyscaleShader;
        private readonly ShaderInstance _gradientCircleShader;

        private BlindableComponent _blindableComponent = default!;

        public BlindOverlay()
        {
            IoCManager.InjectDependencies(this);
            _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").Instance().Duplicate();
            _gradientCircleShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").Instance();
        }
        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<BlindableComponent>(playerEntity, out var blindComp))
                return false;

            _blindableComponent = blindComp;
            return (blindComp.Sources > 0);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            if (_blindableComponent.LightSetup) // Do we need to reset this?
            {
                _lightManager.Enabled = true;
                _blindableComponent.LightSetup = false;
            }

            _blindableComponent.LightSetup = true; // Ok we touched the lights
            _lightManager.Enabled = false;

            _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_greyscaleShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(_gradientCircleShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}

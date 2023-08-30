using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;

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
        private readonly ShaderInstance _circleMaskShader;

        private BlindableComponent _blindableComponent = default!;

        public BlindOverlay()
        {
            IoCManager.InjectDependencies(this);
            _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").InstanceUnique();
            _circleMaskShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
        }
        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
                return false;

            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<BlindableComponent>(playerEntity, out var blindComp))
                return false;

            _blindableComponent = blindComp;

            var blind = _blindableComponent.IsBlind;

            if (!blind && _blindableComponent.LightSetup) // Turn FOV back on if we can see again
            {
                _lightManager.Enabled = true;
                _blindableComponent.LightSetup = false;
                _blindableComponent.GraceFrame = true;
                return true;
            }

            return blind;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            if (!_blindableComponent.GraceFrame)
            {
                _blindableComponent.LightSetup = true; // Ok we touched the lights
                _lightManager.Enabled = false;
            } else
            {
                _blindableComponent.GraceFrame = false;
            }

            _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.UseShader(_greyscaleShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(_circleMaskShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}

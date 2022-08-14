using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;

namespace Content.Client.Eye.Blinding
{
    public sealed class BlurryVisionOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _blurryVisionXShader;
        private readonly ShaderInstance _blurryVisionYShader;
        private BlurryVisionComponent _blurryVisionComponent = default!;

        public BlurryVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _blurryVisionXShader = _prototypeManager.Index<ShaderPrototype>("BlurryVisionX").InstanceUnique();
            _blurryVisionYShader = _prototypeManager.Index<ShaderPrototype>("BlurryVisionY").InstanceUnique();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<BlurryVisionComponent>(playerEntity, out var blurComp))
                return false;

            if (!blurComp.Active)
                return false;

            if (_entityManager.TryGetComponent<BlindableComponent>(playerEntity, out var blindComp)
                && blindComp.Sources > 0)
                return false;

            _blurryVisionComponent = blurComp;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            _blurryVisionXShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _blurryVisionXShader?.SetParameter("BLUR_AMOUNT", (_blurryVisionComponent.Magnitude / 10));
            _blurryVisionYShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _blurryVisionYShader?.SetParameter("BLUR_AMOUNT", (_blurryVisionComponent.Magnitude / 10));

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_blurryVisionXShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(_blurryVisionYShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}

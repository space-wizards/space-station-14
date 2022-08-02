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
        [Dependency] IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _blurryVisionShader;
        private BlurryVisionComponent _blurryVisionComponent = default!;

        public BlurryVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
             _blurryVisionShader = _prototypeManager.Index<ShaderPrototype>("BlurryVision").InstanceUnique();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<BlurryVisionComponent>(playerEntity, out var blurComp))
                return false;

            _blurryVisionComponent = blurComp;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            _blurryVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _blurryVisionShader?.SetParameter("BLUR_AMOUNT", _blurryVisionComponent.Magnitude);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_blurryVisionShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}

using Robust.Client.GameObjects;
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
        private readonly ShaderInstance _dim;
        private BlurryVisionComponent _blurryVisionComponent = default!;

        public BlurryVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _dim = _prototypeManager.Index<ShaderPrototype>("Dim").InstanceUnique();
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

            var opacity = -(_blurryVisionComponent.Magnitude / 15) + 0.9f;

            _dim.SetParameter("DAMAGE_AMOUNT", opacity);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;

            worldHandle.UseShader(_dim);
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.DrawRect(viewport, Color.Black);
            worldHandle.UseShader(null);
        }
    }
}

using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Client.Eye.Blinding
{
    public sealed class BlurryVisionOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private float _magnitude;

        public BlurryVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
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

            if (blurComp.Magnitude <= 0)
                return false;

            if (_entityManager.TryGetComponent<BlindableComponent>(playerEntity, out var blindComp)
                && blindComp.IsBlind)
                return false;

            _magnitude = blurComp.Magnitude;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            // TODO make this better.
            // This is a really shitty effect.
            // Maybe gradually shrink the view-size?
            // Make the effect only apply to the edge of the viewport?
            // Actually make it blurry??
            var opacity =  0.75f * _magnitude / BlurryVisionComponent.MaxMagnitude;
            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.DrawRect(viewport, Color.Black.WithAlpha(opacity));
        }
    }
}

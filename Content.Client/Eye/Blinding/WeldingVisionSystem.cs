using Content.Shared.Eye.Blinding.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Eye.Blinding
{
    public sealed class WeldingVisionSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IOverlayManager _overlaym = default!;
        private WeldingVisionOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WeldingVisionComponent, ComponentInit>(OnWelderInit);
            SubscribeLocalEvent<WeldingVisionComponent, ComponentShutdown>(OnWelderShutdown);

            SubscribeLocalEvent<WeldingVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
            SubscribeLocalEvent<WeldingVisionComponent, LocalPlayerDetachedEvent>(OnDetached);

            _overlay = new();
        }

        private void OnAttached(EntityUid uid, WeldingVisionComponent comp, LocalPlayerAttachedEvent args)
        {
            _overlaym.AddOverlay(_overlay);
        }

        private void OnDetached(EntityUid uid, WeldingVisionComponent comp, LocalPlayerDetachedEvent args)
        {
            _overlaym.RemoveOverlay(_overlay);
        }

        private void OnWelderInit(EntityUid uid, WeldingVisionComponent comp, ComponentInit args)
        {
            if (_player.LocalEntity == uid)
                _overlaym.AddOverlay(_overlay);
        }

        private void OnWelderShutdown(EntityUid uid, WeldingVisionComponent comp, ComponentShutdown args)
        {
            if (_player.LocalEntity == uid)
                _overlaym.RemoveOverlay(_overlay);
        }
    }
}

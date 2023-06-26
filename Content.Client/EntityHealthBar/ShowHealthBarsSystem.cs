using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.EntityHealthBar
{
    public sealed class ShowHealthBarsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private EntityHealthBarOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _overlay = new(EntityManager, _protoMan);
        }

        private void OnInit(EntityUid uid, ShowHealthBarsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlays(component);
            }
        }

        private void OnRemove(EntityUid uid, ShowHealthBarsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowHealthBarsComponent component, PlayerAttachedEvent args)
        {
            ApplyOverlays(component);
        }

        private void ApplyOverlays(ShowHealthBarsComponent component)
        {
            _overlayMan.AddOverlay(_overlay);
            _overlay.DamageContainers.Clear();
            _overlay.DamageContainers.AddRange(component.DamageContainers);
        }

        private void OnPlayerDetached(EntityUid uid, ShowHealthBarsComponent component, PlayerDetachedEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}

using Content.Client.HealthOverlay;
using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.EntityHealthBar
{
    public sealed class ShowHealthBarsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly HealthOverlaySystem _healthOverlaySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnInit(EntityUid uid, ShowHealthBarsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlay(component);
            }
        }

        private void OnRemove(EntityUid uid, ShowHealthBarsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                RemoveOverlay();
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowHealthBarsComponent component, PlayerAttachedEvent args)
        {
            ApplyOverlay(component);
        }

        private void ApplyOverlay(ShowHealthBarsComponent component)
        {
            _healthOverlaySystem.Enabled = true;
            _healthOverlaySystem.ClearDamageContainers();
            _healthOverlaySystem.AddDamageContainers(component.DamageContainers);
        }

        private void OnPlayerDetached(EntityUid uid, ShowHealthBarsComponent component, PlayerDetachedEvent args)
        {
            RemoveOverlay();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }
        private void RemoveOverlay()
        {
            _healthOverlaySystem.Enabled = false;
        }
    }
}

using Content.Shared.Damage;
using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.EntityHealthHud
{
    public sealed class ShowHealthIconsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        private bool _isActive = false;
        public List<string> DamageContainers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowHealthIconsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowHealthIconsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowHealthIconsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowHealthIconsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
            SubscribeLocalEvent<GetStatusIconsEvent>(OnGetStatusIconsEvent);

        }

        public void ApplyOverlays(ShowHealthIconsComponent component)
        {
            _isActive = true;
            DamageContainers.Clear();
            DamageContainers.AddRange(component.DamageContainers);
        }

        public void RemoveOverlay()
        {
            _isActive = false;
        }

        private void OnGetStatusIconsEvent(ref GetStatusIconsEvent @event)
        {
            if (!_isActive)
                return;

            var healthIcons = DecideHealthIcon(@event.Uid);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideHealthIcon(EntityUid uid)
        {
            var result = new List<StatusIconPrototype>();
            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                return result;
            }

            if (!_entManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent) ||
                damageableComponent.DamageContainerID == null ||
                !DamageContainers.Contains(damageableComponent.DamageContainerID))
            {
                return result;
            }

            // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
            if (damageableComponent?.DamageContainerID == "Biological" &&
                _prototypeMan.TryIndex<StatusIconPrototype>("HealthIcon_Fine", out var healthyIcon))
            {
                result.Add(healthyIcon);
            }

            return result;
        }

        private void OnInit(EntityUid uid, ShowHealthIconsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlays(component);
            }
        }

        private void OnRemove(EntityUid uid, ShowHealthIconsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                RemoveOverlay();
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowHealthIconsComponent component, PlayerAttachedEvent args)
        {
            ApplyOverlays(component);
        }

        private void OnPlayerDetached(EntityUid uid, ShowHealthIconsComponent component, PlayerDetachedEvent args)
        {
            RemoveOverlay();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }
    }
}

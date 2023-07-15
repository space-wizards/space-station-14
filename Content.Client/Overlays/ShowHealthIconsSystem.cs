using Content.Shared.Damage;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class ShowHealthIconsSystem : ComponentAddedOverlaySystemBase<ShowHealthIconsComponent>
    {
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        public List<string> DamageContainers = new();

        private StatusIconPrototype? _healthyIcon;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);

        }

        protected override void OnApplyOverlay(ShowHealthIconsComponent component)
        {
            DamageContainers.Clear();
            DamageContainers.AddRange(component.DamageContainers);
        }

        private void OnGetStatusIconsEvent(EntityUid uid, DamageableComponent damageableComponent, ref GetStatusIconsEvent @event)
        {
            if (!IsActive)
                return;

            var healthIcons = DecideHealthIcon(uid, damageableComponent);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideHealthIcon(EntityUid uid, DamageableComponent damageableComponent)
        {
            var result = new List<StatusIconPrototype>();
            if (damageableComponent.DamageContainerID == null ||
                !DamageContainers.Contains(damageableComponent.DamageContainerID))
            {
                return result;
            }

            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                return result;
            }

            // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
            if (damageableComponent?.DamageContainerID == "Biological" &&
                (_healthyIcon != null ||
                _prototypeMan.TryIndex("HealthIcon_Fine", out _healthyIcon)))
            {
                result.Add(_healthyIcon);
            }

            return result;
        }
    }
}

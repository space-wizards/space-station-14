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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);

        }

        protected override void OnApplyOverlay(ShowHealthIconsComponent component)
        {
            base.OnApplyOverlay(component);

            foreach (var damageContainerId in component.DamageContainers)
            {
                if (DamageContainers.Contains(damageContainerId))
                {
                    continue;
                }

                DamageContainers.Add(damageContainerId);
            }
        }

        protected override void OnRemoveOverlay()
        {
            base.OnRemoveOverlay();

            DamageContainers.Clear();
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
                (_prototypeMan.TryIndex<StatusIconPrototype>("HealthIcon_Fine", out var healthyIcon)))
            {
                result.Add(healthyIcon);
            }

            return result;
        }
    }
}

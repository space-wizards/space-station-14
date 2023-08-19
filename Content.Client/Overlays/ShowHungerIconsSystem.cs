using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class ShowHungerIconsSystem : EquipmentHudSystem<ShowHungerIconsComponent>
    {
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HungerComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        }

        private void OnGetStatusIconsEvent(EntityUid uid, HungerComponent hungerComponent, ref GetStatusIconsEvent @event)
        {
            if (!IsActive)
                return;

            var healthIcons = DecideHungerIcon(uid, hungerComponent);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideHungerIcon(EntityUid uid, HungerComponent hungerComponent)
        {
            var result = new List<StatusIconPrototype>();

            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                return result;
            }

            switch (hungerComponent.CurrentThreshold)
            {
                case HungerThreshold.Overfed:
                    if (_prototypeMan.TryIndex<StatusIconPrototype>("HungerIconOverfed", out var overfed))
                    {
                        result.Add(overfed);
                    }
                    break;
                case HungerThreshold.Peckish:
                    if (_prototypeMan.TryIndex<StatusIconPrototype>("HungerIconPeckish", out var peckish))
                    {
                        result.Add(peckish);
                    }
                    break;
                case HungerThreshold.Starving:
                    if (_prototypeMan.TryIndex<StatusIconPrototype>("HungerIconStarving", out var starving))
                    {
                        result.Add(starving);
                    }
                    break;
            }

            return result;
        }
    }
}

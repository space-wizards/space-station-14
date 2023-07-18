using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class ShowHungerIconsSystem : ComponentAddedOverlaySystemBase<ShowHungerIconsComponent>
    {
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        private StatusIconPrototype? _overfed;
        private StatusIconPrototype? _peckish;
        private StatusIconPrototype? _starving;

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
                    if (_overfed != null ||
                        _prototypeMan.TryIndex("HungerIcon_Overfed", out _overfed))
                    {
                        result.Add(_overfed);
                    }
                    break;
                case HungerThreshold.Peckish:
                    if (_peckish != null ||
                        _prototypeMan.TryIndex("HungerIcon_Peckish", out _peckish))
                    {
                        result.Add(_peckish);
                    }
                    break;
                case HungerThreshold.Starving:
                    if (_starving != null ||
                        _prototypeMan.TryIndex("HungerIcon_Starving", out _starving))
                    {
                        result.Add(_starving);
                    }
                    break;
            }

            return result;
        }
    }
}

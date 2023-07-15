using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class ShowThirstIconsSystem : ComponentAddedOverlaySystemBase<ShowThirstIconsComponent>
    {
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        private StatusIconPrototype? _overhydrated;
        private StatusIconPrototype? _thirsty;
        private StatusIconPrototype? _parched;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ThirstComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        }

        private void OnGetStatusIconsEvent(EntityUid uid, ThirstComponent thirstComponent, ref GetStatusIconsEvent @event)
        {
            if (!IsActive)
                return;

            var healthIcons = DecideThirstIcon(uid, thirstComponent);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideThirstIcon(EntityUid uid, ThirstComponent thirstComponent)
        {
            var result = new List<StatusIconPrototype>();

            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                return result;
            }

            switch (thirstComponent.CurrentThirstThreshold)
            {
                case ThirstThreshold.OverHydrated:
                    if (_overhydrated != null ||
                        _prototypeMan.TryIndex("ThirstIcon_Overhydrated", out _overhydrated))
                    {
                        result.Add(_overhydrated);
                    }
                    break;
                case ThirstThreshold.Thirsty:
                    if (_thirsty != null ||
                        _prototypeMan.TryIndex("ThirstIcon_Thirsty", out _thirsty))
                    {
                        result.Add(_thirsty);
                    }
                    break;
                case ThirstThreshold.Parched:
                    if (_parched != null ||
                        _prototypeMan.TryIndex("ThirstIcon_Parched", out _parched))
                    {
                        result.Add(_parched);
                    }
                    break;
            }

            return result;
        }
    }
}

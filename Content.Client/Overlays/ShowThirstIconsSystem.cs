namespace Content.Client.Overlays;

using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

public sealed class ShowThirstIconsSystem : EquipmentHudSystem<ShowThirstIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

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
            (metaDataComponent.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
        {
            return result;
        }

        switch (thirstComponent.CurrentThirstThreshold)
        {
            case ThirstThreshold.OverHydrated:
                if (_prototypeMan.TryIndex<StatusIconPrototype>("ThirstIconOverhydrated", out var overhydrated))
                {
                    result.Add(overhydrated);
                }
                break;
            case ThirstThreshold.Thirsty:
                if (_prototypeMan.TryIndex<StatusIconPrototype>("ThirstIconThirsty", out var thirsty))
                {
                    result.Add(thirsty);
                }
                break;
            case ThirstThreshold.Parched:
                if (_prototypeMan.TryIndex<StatusIconPrototype>("ThirstIconParched", out var parched))
                {
                    result.Add(parched);
                }
                break;
        }

        return result;
    }
}

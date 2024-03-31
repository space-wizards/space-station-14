using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowThirstIconsSystem : EquipmentHudSystem<ShowThirstIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThirstComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, ThirstComponent thirstComponent, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
            return;

        var thirstIcons = DecideThirstIcon(uid, thirstComponent);

        args.StatusIcons.AddRange(thirstIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideThirstIcon(EntityUid uid, ThirstComponent thirstComponent)
    {
        var result = new List<StatusIconPrototype>();

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

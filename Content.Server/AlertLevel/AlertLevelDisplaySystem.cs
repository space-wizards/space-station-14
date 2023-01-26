using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Robust.Server.GameObjects;

namespace Content.Server.AlertLevel;

public sealed class AlertLevelDisplaySystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertChanged);
        SubscribeLocalEvent<AlertLevelDisplayComponent, ComponentInit>(OnDisplayInit);
    }

    private void OnAlertChanged(AlertLevelChangedEvent args)
    {
        foreach (var (_, appearance) in EntityManager.EntityQuery<AlertLevelDisplayComponent, AppearanceComponent>())
        {
            _appearance.SetData(appearance.Owner, AlertLevelDisplay.CurrentLevel, args.AlertLevel, appearance);
        }
    }

    private void OnDisplayInit(EntityUid uid, AlertLevelDisplayComponent component, ComponentInit args)
    {
        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null && TryComp(stationUid, out AlertLevelComponent? alert))
            {
                _appearance.SetData(uid, AlertLevelDisplay.CurrentLevel, alert.CurrentLevel, appearance);
            }
        }
    }
}

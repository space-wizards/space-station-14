using Content.Server.AlertLevel;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;

namespace Content.Server.AlertLevel.Systems;

public sealed class AlertLevelChangeOnSpawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangeOnSpawnComponent, MapInitEvent>(OnInit);
    }

    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private void OnInit(EntityUid uid, AlertLevelChangeOnSpawnComponent comp, MapInitEvent args)
    {
        var stationuid = _station.GetOwningStation(uid);
        if (!stationuid.HasValue)
            return;

        if (comp.Level == null)
            return;

        if (_alertLevelSystem.GetLevel(stationuid.Value) != "green")
            return;

        _alertLevelSystem.SetLevel(stationuid.Value, comp.Level, true, true, true);
    }
}

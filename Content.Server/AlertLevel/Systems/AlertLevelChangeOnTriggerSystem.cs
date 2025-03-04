using Content.Server.AlertLevel;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Station.Systems;


namespace Content.Server.AlertLevel.Systems;

public sealed class AlertLevelChangeOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private void OnTrigger(EntityUid uid, AlertLevelChangeOnTriggerComponent ent, ref TriggerEvent args)
    {
        var stationuid = _station.GetOwningStation(uid);
        if (!stationuid.HasValue)
            return;

        if (ent.Level == null)
            return;

        if (!TryComp<AlertLevelComponent>(stationuid.Value, out var alertLevelComponent))
            return;

        if (_alertLevelSystem.GetLevel(stationuid.Value) != _alertLevelSystem.GetDefaultLevel(stationuid.Value))
        {
            return;
        }

        _alertLevelSystem.SetLevel(stationuid.Value, ent.Level, true, true, true);
    }
}

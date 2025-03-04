using Content.Server.AlertLevel;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.AlertLevel.Systems;

public sealed class AlertLevelChangeOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangeOnTriggerComponent, MapInitEvent>(OnMapInit);
    }

    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private void OnMapInit(EntityUid uid, AlertLevelChangeOnTriggerComponent ent, ref MapInitEvent args)
    {
        var stationuid = _station.GetOwningStation(uid);
        if (!stationuid.HasValue)
            return;

        if (ent.Level == null)
            return;

        if (!TryComp<AlertLevelComponent>(stationuid.Value, out var alertLevelComponent))
            return;

        if (!_prototypeManager.TryIndex(alertLevelComponent.AlertLevelPrototype, out AlertLevelPrototype? alerts))
        {
            return;
        }

        alertLevelComponent.AlertLevels = alerts;

        var defaultLevel = alertLevelComponent.AlertLevels.DefaultLevel;
        if (string.IsNullOrEmpty(defaultLevel))
        {
            defaultLevel = alertLevelComponent.AlertLevels.Levels.Keys.First();
        }

        if (_alertLevelSystem.GetLevel(stationuid.Value) != defaultLevel)
            return;

        _alertLevelSystem.SetLevel(stationuid.Value, ent.Level, true, true, true);
    }
}

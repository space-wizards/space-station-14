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

    private void OnTrigger(Entity<AlertLevelChangeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var stationuid = _station.GetOwningStation(ent.Owner);
        if (!stationuid.HasValue)
            return;

        _alertLevelSystem.SetLevel(stationuid.Value, ent.Comp.Level, true, true, true);
    }
}

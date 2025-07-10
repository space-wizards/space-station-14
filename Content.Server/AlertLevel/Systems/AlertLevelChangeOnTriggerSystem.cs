using Content.Server.AlertLevel;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Station.Systems;

namespace Content.Server.AlertLevel.Systems;

public sealed class AlertLevelChangeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<AlertLevelChangeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var stationUid = _station.GetOwningStation(ent.Owner);
        if (!stationUid.HasValue)
            return;

        _alertLevelSystem.SetLevel(stationUid.Value, ent.Comp.Level, ent.Comp.PlaySound, ent.Comp.Announce, ent.Comp.Force);
    }
}

using Content.Server.AlertLevel;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Server.Station.Systems;

namespace Content.Server.Trigger.Systems;

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
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var stationUid = _station.GetOwningStation(ent.Owner);
        if (stationUid == null)
            return;

        _alertLevelSystem.SetLevel(stationUid.Value, ent.Comp.Level, ent.Comp.PlaySound, ent.Comp.Announce, ent.Comp.Force);
        args.Handled = true;
    }
}

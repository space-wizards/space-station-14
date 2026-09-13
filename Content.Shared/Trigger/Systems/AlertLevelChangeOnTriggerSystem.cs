using Content.Shared.AlertLevel;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Station;

namespace Content.Shared.Trigger.Systems;

public sealed partial class AlertLevelChangeOnTriggerSystem : XOnTriggerSystem<AlertLevelChangeOnTriggerComponent>
{
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private SharedStationSystem _station = default!;

    protected override void OnTrigger(Entity<AlertLevelChangeOnTriggerComponent> ent, EntityUid _, ref TriggerEvent args)
    {
        var stationUid = _station.GetOwningStation(ent.Owner);
        if (stationUid == null)
            return;

        _alertLevel.SetLevel(
            stationUid.Value,
            ent.Comp.Level,
            playSound: ent.Comp.PlaySound,
            announce: ent.Comp.Announce,
            force: ent.Comp.Force);

        args.Handled = true;
    }
}

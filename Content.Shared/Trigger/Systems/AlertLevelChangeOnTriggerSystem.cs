using Content.Shared.AlertLevel;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Station;

namespace Content.Shared.Trigger.Systems;

public sealed class AlertLevelChangeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

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

        _alertLevel.SetLevel(
            stationUid.Value,
            ent.Comp.Level,
            playSound: ent.Comp.PlaySound,
            announce: ent.Comp.Announce,
            force: ent.Comp.Force);

        args.Handled = true;
    }
}

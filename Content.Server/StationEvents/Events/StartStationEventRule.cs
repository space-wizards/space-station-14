using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

//TO DO: delete and forget about this as the worst joke ever.
public sealed class StartStationEventRule : StationEventSystem<StartStationEventRuleComponent>
{
    protected override void Started(EntityUid uid, StartStationEventRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;
        base.Started(uid, component, gameRule, args);
    }
}
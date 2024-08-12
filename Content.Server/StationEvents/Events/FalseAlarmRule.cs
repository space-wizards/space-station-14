using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class FalseAlarmRule : StationEventSystem<FalseAlarmRuleComponent>
{
    [Dependency] private readonly EventManagerSystem _event = default!;

    protected override void Started(EntityUid uid, FalseAlarmRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var allEv = _event.AllEvents().Select(p => p.Value).ToList();
        var picked = RobustRandom.Pick(allEv);

        stationEvent.StartAnnouncement = picked.StartAnnouncement;
        stationEvent.StartAudio = picked.StartAudio;
        stationEvent.StartAnnouncementColor = picked.StartAnnouncementColor;

        base.Started(uid, component, gameRule, args);
    }
}

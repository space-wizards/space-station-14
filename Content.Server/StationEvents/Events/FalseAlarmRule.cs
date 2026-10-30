using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that mimic another event with a false announcement.
/// </summary>
/// <seealso cref="FalseAlarmRuleComponent"/>
[UsedImplicitly]
public sealed partial class FalseAlarmRule : StationEventSystem<FalseAlarmRuleComponent>
{
    [Dependency] private EventManagerSystem _event = default!;

    protected override void Started(Entity<FalseAlarmRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        if (!TryComp<StationEventComponent>(ent, out var stationEvent))
            return;

        var allEv = _event.AllEvents().Select(p => p.Value).ToList();
        var picked = RobustRandom.Pick(allEv);

        stationEvent.StartAnnouncement = picked.StartAnnouncement;
        stationEvent.StartAudio = picked.StartAudio;
        stationEvent.StartAnnouncementColor = picked.StartAnnouncementColor;

        base.Started(ent, ref args);
    }
}

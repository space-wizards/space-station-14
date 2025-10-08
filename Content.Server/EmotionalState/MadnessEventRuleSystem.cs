using System;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Radio;
using Robust.Shared.Random;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.EmotionalState;
using Content.Server.Shuttles.Events;

namespace Content.Server.StationEvents.Events;

public sealed class MadnessEventRule : StationEventSystem<MadnessEventRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, MadnessEventRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var lightQuery = EntityQueryEnumerator<EmotionalStateComponent>();
        while (lightQuery.MoveNext(out var item))
        {
            if (_random.NextDouble() < comp.ChanceOfMadness)
            {
                item.LastAuthoritativeEmotionalValue -= (float) _random.NextDouble(item.Thresholds[EmotionalThreshold.Demonic], item.Thresholds[EmotionalThreshold.Rainbow]);
            }
        }
    }
}

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
using Content.Shared.Shuttles.Systems;
using Content.Server.Shuttles.Systems;
using Content.Server.GameTicking.Rules;

namespace Content.Server.StationEvents.Events;

public sealed class MadnessShuttleEventRule : StationEventSystem<MadnessShuttleEventRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MadnessEventRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }

    private void OnRuleLoadedGrids(Entity<MadnessEventRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        _shuttle.TryAddFTLDestination(args.Map, true, false, false, out _);
    }

    protected override void Started(EntityUid uid, MadnessShuttleEventRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);
    }
}

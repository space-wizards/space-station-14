using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents;

/// <summary>
/// This handles scheduling and launching meteors at a station at regular intervals.
/// TODO: there is 100% a world in which this is genericized and can be used for lots of basic event scheduling
/// </summary>
public sealed class MeteorSchedulerSystem : GameRuleSystem<MeteorSchedulerComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected override void Started(EntityUid uid, MeteorSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextSwarmTime = Timing.CurTime + RobustRandom.Next(component.MinSwarmDelay, component.MaxSwarmDelay);
    }

    protected override void ActiveTick(EntityUid uid, MeteorSchedulerComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextSwarmTime)
            return;
        RunSwarm((uid, component));
        component.NextSwarmTime += RobustRandom.Next(component.MinSwarmDelay, component.MaxSwarmDelay);
    }

    private void RunSwarm(Entity<MeteorSchedulerComponent> ent)
    {
        var swarmWeights = _prototypeManager.Index(ent.Comp.Config);
        GameTicker.StartGameRule(swarmWeights.Pick(RobustRandom));
    }
}

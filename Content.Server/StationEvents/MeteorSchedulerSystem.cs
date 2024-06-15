using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents;

/// <summary>
/// This handles scheduling and launching meteors at a station at regular intervals.
/// TODO: there is 100% a world in which this is genericized and can be used for lots of basic event scheduling
/// </summary>
public sealed class MeteorSchedulerSystem : GameRuleSystem<MeteorSchedulerComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TimeSpan _meteorMinDelay;
    private TimeSpan _meteorMaxDelay;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.MeteorSwarmMinTime, f => { _meteorMinDelay = TimeSpan.FromMinutes(f); }, true);
        _cfg.OnValueChanged(CCVars.MeteorSwarmMaxTime, f => { _meteorMaxDelay = TimeSpan.FromMinutes(f); }, true);
    }

    protected override void Started(EntityUid uid, MeteorSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextSwarmTime = Timing.CurTime + RobustRandom.Next(_meteorMinDelay, _meteorMaxDelay);
    }

    protected override void ActiveTick(EntityUid uid, MeteorSchedulerComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextSwarmTime)
            return;
        RunSwarm((uid, component));

        component.NextSwarmTime += RobustRandom.Next(_meteorMinDelay, _meteorMaxDelay);
    }

    private void RunSwarm(Entity<MeteorSchedulerComponent> ent)
    {
        var swarmWeights = _prototypeManager.Index(ent.Comp.Config);
        GameTicker.StartGameRule(swarmWeights.Pick(RobustRandom));
    }
}

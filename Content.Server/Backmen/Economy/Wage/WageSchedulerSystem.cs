// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server.Backmen.Economy.Wage;

[RegisterComponent, Access(typeof(WageSchedulerSystem))]
public sealed partial class WageSchedulerRuleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public float TimeUntilNextWage;

    [ViewVariables(VVAccess.ReadWrite)] public float MinimumTimeUntilFirstWage { get; set; } = 900;
    [ViewVariables(VVAccess.ReadWrite)] public float WageInterval { get; set; } = 1800;

    public WageSchedulerRuleComponent()
    {
        TimeUntilNextWage = MinimumTimeUntilFirstWage;
    }
}


public sealed class WageSchedulerSystem : GameRuleSystem<WageSchedulerRuleComponent>
{
    //[Dependency] private readonly IChatManager _chatManager = default!;
    //public override string Prototype => "WageScheduler";
    [Dependency] private readonly WageManagerSystem _wageManagerSystem = default!;



    protected override void Started(EntityUid uid, WageSchedulerRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        //_chatManager.DispatchServerAnnouncement(Loc.GetString("rule-wage-announcement"));
    }
    protected override void Ended(EntityUid uid, WageSchedulerRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        component.TimeUntilNextWage = component.MinimumTimeUntilFirstWage;
    }

    protected override void ActiveTick(EntityUid uid, WageSchedulerRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!_wageManagerSystem.WagesEnabled)
            return;
        if (component.TimeUntilNextWage > 0)
        {
            component.TimeUntilNextWage -= frameTime;
            return;
        }
        QueueLocalEvent(new WagePaydayEvent());
        component.TimeUntilNextWage = component.WageInterval;
    }
}

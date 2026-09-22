using Content.Server.RoundEnd;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerXenoborgsRuleSystem : XenoborgsRuleSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private AliveHumanoidTargetSystem _target = default!;

    protected override void ActiveTick(EntityUid uid, XenoborgsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!component.NextRoundEndCheck.HasValue || component.NextRoundEndCheck > _timing.CurTime)
            return;

        CheckRoundEnd(component);
        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    private void CheckRoundEnd(XenoborgsRuleComponent xenoborgsRuleComponent)
    {
        var numXenoborgs = GetNumberXenoborgs();
        var numHumans = _target.GetMinds().Count;

        xenoborgsRuleComponent.MaxNumberXenoborgs = Math.Max(xenoborgsRuleComponent.MaxNumberXenoborgs, numXenoborgs);

        if (xenoborgsRuleComponent.XenoborgShuttleCalled
            || (float)numXenoborgs / (numHumans + numXenoborgs) <= xenoborgsRuleComponent.XenoborgShuttleCallPercentage
            || _roundEnd.IsRoundEndRequested())
            return;

        GameTicker.StationAnnouncement("xenoborg-shuttle-call", color: Color.BlueViolet);
        _roundEnd.RequestRoundEnd(null, null, false, cantRecall: true);
        xenoborgsRuleComponent.XenoborgShuttleCalled = true;
    }
}

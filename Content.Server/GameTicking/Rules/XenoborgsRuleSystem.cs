using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Destructible;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly Color AnnouncmentColor = Color.Gold;

    public void SendXenoborgDeathAnnouncement(Entity<XenoborgsRuleComponent> ent, bool mothershipCoreAlive)
    {
        if (ent.Comp.MothershipCoreDeathAnnouncmentSent)
            return;

        var status = mothershipCoreAlive ? "alive" : "dead";
        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString($"xenoborgs-no-more-threat-mothership-core-{status}-announcement"),
            colorOverride: AnnouncmentColor);
    }

    public void SendMothershipDeathAnnouncement(Entity<XenoborgsRuleComponent> ent)
    {
        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("mothership-destroyed-announcement"),
            colorOverride: AnnouncmentColor);

        ent.Comp.MothershipCoreDeathAnnouncmentSent = true;
    }

    // TODO: Refactor the end of round text
    protected override void AppendRoundEndText(EntityUid uid,
        XenoborgsRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var numXenoborgs = GetNumberXenoborgs();
        var numHumans = _mindSystem.GetAliveHumans().Count;

        if (numXenoborgs < 5)
            args.AddLine(Loc.GetString("xenoborgs-crewmajor"));
        else if (4 * numXenoborgs < numHumans)
            args.AddLine(Loc.GetString("xenoborgs-crewmajor"));
        else if (2 * numXenoborgs < numHumans)
            args.AddLine(Loc.GetString("xenoborgs-crewminor"));
        else if (1.5 * numXenoborgs < numHumans)
            args.AddLine(Loc.GetString("xenoborgs-neutral"));
        else if (numXenoborgs < numHumans)
            args.AddLine(Loc.GetString("xenoborgs-borgsminor"));
        else
            args.AddLine(Loc.GetString("xenoborgs-borgsmajor"));

        var numMothershipCores = GetNumberMothershipCores();

        if (numMothershipCores == 0)
            args.AddLine(Loc.GetString("xenoborgs-cond-all-xenoborgs-dead-core-dead"));
        else if (numXenoborgs == 0)
            args.AddLine(Loc.GetString("xenoborgs-cond-all-xenoborgs-dead-core-alive"));
        else
        {
            args.AddLine(Loc.GetString("xenoborg-number-xenoborg-alive-end", ("count", numXenoborgs)));
            args.AddLine(Loc.GetString("xenoborg-number-crew-alive-end", ("count", numHumans)));
        }

        args.AddLine(Loc.GetString("xenoborg-max-number", ("count", component.MaxNumberXenoborgs)));

        args.AddLine(Loc.GetString("xenoborgs-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("xenoborgs-list", ("name", name), ("user", sessionData.UserName)));
        }
        args.AddLine("");
    }

    private void CheckRoundEnd(XenoborgsRuleComponent xenoborgsRuleComponent)
    {
        var numXenoborgs = GetNumberXenoborgs();
        var numHumans = _mindSystem.GetAliveHumans().Count;

        xenoborgsRuleComponent.MaxNumberXenoborgs = Math.Max(xenoborgsRuleComponent.MaxNumberXenoborgs, numXenoborgs);

        if (xenoborgsRuleComponent.XenoborgShuttleCalled
            || (float)numXenoborgs / (numHumans + numXenoborgs) <= xenoborgsRuleComponent.XenoborgShuttleCallPercentage
            || _roundEnd.IsRoundEndRequested())
            return;

        foreach (var station in _station.GetStations())
        {
            _chatSystem.DispatchStationAnnouncement(station, Loc.GetString("xenoborg-shuttle-call"), colorOverride: Color.BlueViolet);
        }
        _roundEnd.RequestRoundEnd(null, false, cantRecall: true);
        xenoborgsRuleComponent.XenoborgShuttleCalled = true;
    }

    protected override void Started(EntityUid uid, XenoborgsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    protected override void ActiveTick(EntityUid uid, XenoborgsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!component.NextRoundEndCheck.HasValue || component.NextRoundEndCheck > _timing.CurTime)
            return;

        CheckRoundEnd(component);
        component.NextRoundEndCheck = _timing.CurTime + component.EndCheckDelay;
    }

    /// <summary>
    /// Get the number of xenoborgs
    /// </summary>
    /// <param name="playerControlled">if it should only include xenoborgs with a mind</param>
    /// <param name="alive">if it should only include xenoborgs that are alive</param>
    /// <returns>the number of xenoborgs</returns>
    private int GetNumberXenoborgs(bool playerControlled = true, bool alive = true)
    {
        var numberXenoborgs = 0;

        var query = EntityQueryEnumerator<XenoborgComponent>();
        while (query.MoveNext(out var xenoborg, out _))
        {
            if (HasComp<MothershipCoreComponent>(xenoborg))
                continue;

            if (playerControlled && !_mindSystem.TryGetMind(xenoborg, out _, out _))
                continue;

            if (alive && !_mobState.IsAlive(xenoborg))
                continue;

            numberXenoborgs++;
        }

        return numberXenoborgs;
    }

    /// <summary>
    /// Gets the number of xenoborg cores
    /// </summary>
    /// <returns>the number of xenoborg cores</returns>
    private int GetNumberMothershipCores()
    {
        var numberMothershipCores = 0;

        var mothershipCoreQuery = EntityQueryEnumerator<MothershipCoreComponent>();
        while (mothershipCoreQuery.MoveNext(out _, out _))
        {
            numberMothershipCores++;
        }

        return numberMothershipCores;
    }
}

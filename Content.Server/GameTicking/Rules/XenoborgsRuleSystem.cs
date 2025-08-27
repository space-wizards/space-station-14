using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.RoundEnd;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Player;
using System.Globalization;

namespace Content.Server.GameTicking.Rules;

public sealed class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoborgsRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntSelected);
    }

    protected override void Started(EntityUid uid,
        XenoborgsRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    private void OnAfterAntagEntSelected(Entity<XenoborgsRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (TryComp<XenoborgComponent>(args.EntityUid, out _))
        {
            _antag.SendBriefing(args.Session,
                Loc.GetString("xenoborgs-welcome"),
                Color.BlueViolet,
                ent.Comp.GreetSoundNotification);
        }
        else if (TryComp<MothershipCoreComponent>(args.EntityUid, out _))
        {
            _antag.SendBriefing(args.Session,
                Loc.GetString("mothership-welcome"),
                Color.BlueViolet,
                ent.Comp.GreetSoundNotification);
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        XenoborgsRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var numXenoborgs = GetNumberXenoborgs();
        var numHumans = GetNumberHumans();

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
            args.AddLine(Loc.GetString("xenoborgs-cond-xenoborgs-alive", ("count", numXenoborgs)));

        args.AddLine(Loc.GetString("xenoborgs-list-start"));

        var antags = _antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("xenoborgs-list", ("name", name), ("user", sessionData.UserName)));
        }
    }

    /// <summary>
    /// Get the number of xenoborgs
    /// </summary>
    /// <param name="playerControlled">if it should only include xenoborgs with a mind</param>
    /// <param name="alive">if it should only include xenoborgs that are alive</param>
    /// <returns></returns>
    private int GetNumberXenoborgs(bool playerControlled = true, bool alive = true)
    {
        var numberXenoborgs = 0;

        var query = AllEntityQuery<XenoborgComponent>();
        while (query.MoveNext(out var xenoborg, out _))
        {
            if (playerControlled && !_mindSystem.TryGetMind(xenoborg, out _, out _))
                continue;

            if (alive && !_mobState.IsAlive(xenoborg))
                continue;

            numberXenoborgs++;
        }

        return numberXenoborgs;
    }

    /// <summary>
    /// Gets the number of humans who are alive
    /// </summary>
    /// <returns></returns>
    private int GetNumberHumans()
    {
        var humans = 0;

        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent>();
        while (players.MoveNext(out var uid, out _, out _, out var mob))
        {
            if (!_mobState.IsAlive(uid, mob))
                continue;

            humans++;
        }

        return humans;
    }

    /// <summary>
    /// Gets the number of xenoborg cores
    /// </summary>
    /// <returns></returns>
    private int GetNumberMothershipCores()
    {
        var numberMothershipCores = 0;

        var mothershipCoreQuery = AllEntityQuery<MothershipCoreComponent>();
        while (mothershipCoreQuery.MoveNext(out _, out _))
        {
            numberMothershipCores++;
        }

        return numberMothershipCores;
    }

}

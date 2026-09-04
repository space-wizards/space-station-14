using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Systems;
using Content.Shared.Xenoborgs.Components;

namespace Content.Shared.GameTicking.Rules;

public abstract partial class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    [Dependency] private AliveHumanoidTargetSystem _target = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;

    private static readonly Color AnnouncmentColor = Color.Gold;

     public void SendXenoborgDeathAnnouncement(Entity<XenoborgsRuleComponent> ent, bool mothershipCoreAlive)
    {
        if (ent.Comp.MothershipCoreDeathAnnouncmentSent)
            return;

        var status = mothershipCoreAlive ? "alive" : "dead";
        GameTicker.GameAnnouncement($"xenoborgs-no-more-threat-mothership-core-{status}-announcement", color: AnnouncmentColor);
    }

    public void SendMothershipDeathAnnouncement(Entity<XenoborgsRuleComponent> ent)
    {
        GameTicker.GameAnnouncement("mothership-destroyed-announcement", color: AnnouncmentColor);
        ent.Comp.MothershipCoreDeathAnnouncmentSent = true;
    }

    // TODO: Refactor the end of round text
    protected override void AppendRoundEndText(Entity<XenoborgsRuleComponent> rule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(rule, ref args);

        var numXenoborgs = GetNumberXenoborgs();
        var numHumans = _target.GetMinds().Count;

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

        args.AddLine(Loc.GetString("xenoborg-max-number", ("count", rule.Comp.MaxNumberXenoborgs)));

        args.AddLine(Loc.GetString("xenoborgs-list-start"));

        var antags = _antag.GetAntagIdentifiers(rule.Owner);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("xenoborgs-list", ("name", name), ("user", sessionData.UserName)));
        }
        args.AddLine("");
    }


    protected override void Started(Entity<XenoborgsRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        rule.Comp1.NextRoundEndCheck = Timing.CurTime + rule.Comp1.EndCheckDelay;
    }

    /// <summary>
    /// Get the number of xenoborgs
    /// </summary>
    /// <param name="playerControlled">if it should only include xenoborgs with a mind</param>
    /// <param name="alive">if it should only include xenoborgs that are alive</param>
    /// <returns>the number of xenoborgs</returns>
    protected int GetNumberXenoborgs(bool playerControlled = true, bool alive = true)
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

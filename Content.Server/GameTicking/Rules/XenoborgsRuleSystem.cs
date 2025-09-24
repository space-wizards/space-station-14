using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Destructible;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    private static readonly Color ANNOUNCMENT_COLOR = Color.Gold;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoborgComponent, DestructionEventArgs>(OnXenoborgDestroyed);
    }

    private void OnXenoborgDestroyed(EntityUid ent, XenoborgComponent component, DestructionEventArgs args)
    {
        // if a xenoborg is destroyed, it will check if other xenoborgs are destroyed
        var xenoborgQuery = AllEntityQuery<XenoborgComponent>();
        while (xenoborgQuery.MoveNext(out var xenoborgEnt, out _))
        {
            // if it finds another xenoborg that is different from the one just destroyed,
            // it means there are still more xenoborgs.
            if (xenoborgEnt != ent)
                return;
        }

        // all xenoborgs are gone
        var mothershipCoreQuery = AllEntityQuery<MothershipCoreComponent>();
        var status = mothershipCoreQuery.MoveNext(out _, out _) ? "alive" : "dead";

        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString($"xenoborgs-no-more-threat-mothership-core-{status}-announcement"),
            colorOverride: ANNOUNCMENT_COLOR);
    }

    public void SendMothershipDeathAnnouncement()
    {
        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("mothership-destroyed-announcement"),
            colorOverride: ANNOUNCMENT_COLOR);
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
    /// <returns>the number of xenoborgs</returns>
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
    /// Gets the number of xenoborg cores
    /// </summary>
    /// <returns>the number of xenoborg cores</returns>
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

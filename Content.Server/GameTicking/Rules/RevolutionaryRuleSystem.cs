using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Cuffs.Components;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    //Used in OnPostFlash, no reference to the rule component is available
    public readonly ProtoId<NpcFactionPrototype> RevolutionaryNpcFaction = "Revolutionary";
    public readonly ProtoId<NpcFactionPrototype> RevPrototypeId = "Rev";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);

        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);

        SubscribeLocalEvent<RevolutionaryRoleComponent, GetBriefingEvent>(OnGetBriefing);

    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }

    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (component.CommandCheck <= _timing.CurTime)
        {
            component.CommandCheck = _timing.CurTime + component.TimerWait;

            if (CheckCommandLose())
            {
                _roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, component.ShuttleCallTime);
                GameTicker.EndGameRule(uid, gameRule);
            }
        }
    }

    private bool IsGroupEscaping(List<EntityUid> list)
    {
        foreach (var entity in list)
        {
            if (_emergencyShuttle.IsTargetEscaping(entity) && !(TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0))
            {
                return true;
            }
        }
        return false;
    }

    private string GetWinType()
    {
        if (IsGroupDetainedOrDead(GetHeadRevList(), false, false, false))
            return "rev-crew-major";

        var isCommandEscaping = IsGroupEscaping(GetCommandList(false));
        if (GetRevolutionaryPercentage(GetCommandList()) >= 0.5 && !isCommandEscaping)
            return "rev-major";

        var revsMinor = false;
        var crewMinor = false;

        if (!isCommandEscaping)
            revsMinor = true;

        if (!IsGroupEscaping(GetHeadRevList()))
            crewMinor = true;

        if (revsMinor && !crewMinor)
            return "rev-minor";
        if (!revsMinor && crewMinor)
            return "rev-crew-minor";

        return "rev-draw";
    }

    /// <summary>
    /// Returns the number of entities in a list that are escaping to Central Command alive and unrestrained.
    /// </summary>
    /// <param name="list">The list to get the number of escapees from.</param>
    /// <returns></returns>
    private int GetEscapeCount(List<EntityUid> list)
    {
        var escapes = 0;
        foreach (var entity in list)
        {
            if (_emergencyShuttle.IsTargetEscaping(entity) && !(TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0))
            {
                escapes++;
            }
        }

        return escapes;
    }

    protected override void AppendRoundEndText(EntityUid uid,
        RevolutionaryRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var winType = GetWinType();
        args.AddLine(Loc.GetString(winType));

        var sessionData = _antag.GetAntagIdentifiers(uid);

        if (winType == "rev-crew-major")
            args.AddLine(Loc.GetString("all-revs-failed"));
        else
        {
            var escapedHeadRevs = GetEscapeCount(GetHeadRevList());
            var commandRevPercent = GetRevolutionaryPercentage(GetCommandList());
            args.AddLine(Loc.GetString("rev-crew-percentage",
            ("percentage", GetRevolutionaryPercentage(GetCrewList()) * 100),
            ("color", winType == "crew-minor" ? "green" : "yellow")
            ));
            args.AddLine(Loc.GetString("rev-command-percentage",
            ("percentage", commandRevPercent * 100),
            ("color", commandRevPercent >= 0.5 || winType == "rev-crew-minor" ? "green" : "red")
            ));
            args.AddLine(Loc.GetString(GetEscapeCount(GetCommandList(false)) == 1 ? "rev-loyal-command-singular" : "rev-loyal-command",
            ("count", GetEscapeCount(GetCommandList(false))),
            ("color", winType == "rev-crew-minor" ? "green" : "red")
            ));
            args.AddLine(Loc.GetString(GetEscapeCount(GetHeadRevList()) == 1 ? "headrev-escapes-singular" : "headrev-escapes",
            ("count", escapedHeadRevs),
            ("color", winType == "rev-crew-minor" || escapedHeadRevs == 0 ? "red" : "green")
            ));
        }

        args.AddLine(Loc.GetString("rev-headrev-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            _role.MindHasRole<RevolutionaryRoleComponent>(mind, out var role);
            var count = CompOrNull<RevolutionaryRoleComponent>(role)?.ConvertedCount ?? 0;

            args.AddLine(Loc.GetString("rev-headrev-name-user",
                ("name", name),
                ("username", data.UserName),
                ("count", count)));

            // TODO: someone suggested listing all alive? revs maybe implement at some point
        }
        args.AddLine("");
    }

    private void OnGetBriefing(EntityUid uid, RevolutionaryRoleComponent comp, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;
        var head = HasComp<HeadRevolutionaryComponent>(ent);
        args.Append(Loc.GetString(head ? "head-rev-briefing" : "rev-briefing"));
    }

    /// <summary>
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    private void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        if (uid != ev.User || !ev.Melee)
            return;

        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target);

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidProfileComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, RevolutionaryNpcFaction);
        var revComp = EnsureComp<RevolutionaryComponent>(ev.Target);

        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind,
                LogImpact.Medium,
                $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");

            if (_mind.TryGetMind(ev.User.Value, out var revMindId, out _))
            {
                if (_role.MindHasRole<RevolutionaryRoleComponent>(revMindId, out var role))
                {
                    role.Value.Comp2.ConvertedCount++;
                    Dirty(role.Value.Owner, role.Value.Comp2);
                }
            }
        }

        if (mindId == default || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleRevolutionary");
        }

        if (mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
            _antag.SendBriefing(session, Loc.GetString("rev-role-greeting"), Color.Red, revComp.RevStartSound);
    }

    //TODO: Enemies of the revolution
    private void OnCommandMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose();
    }

    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    /// is it supposed to remove all sec and command jobs because it doesn't and it didn't pre-rework either
    private bool CheckCommandLose()
    {
        return IsGroupDetainedOrDead(GetCommandList(), true, true, true);
    }

    /// <summary>
    /// Returns a list of all humanoid player-controlled entities, with the exception of Nuclear Operatives, Space Ninjas and Wizards.
    /// </summary>
    /// <returns></returns>
    private List<EntityUid> GetCrewList()
    {
        var crewList = new List<EntityUid>();
        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent>();
        while (players.MoveNext(out var uid, out _, out _))
        {
            if (HasComp<NukeopsRoleComponent>(uid) || HasComp<NinjaRoleComponent>(uid) || HasComp<WizardRoleComponent>(uid))
                continue;
            crewList.Add(uid);
        }

        return crewList;
    }
    private List<EntityUid> GetCommandList(bool countRevs = true)
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            if (!countRevs && (HasComp<RevolutionaryComponent>(id) || HasComp<HeadRevolutionaryComponent>(id)))
                continue;
            commandList.Add(id);
        }

        return commandList;
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose();
    }

    private List<EntityUid> GetHeadRevList()
    {
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var uid, out _, out _))
        {
            headRevList.Add(uid);
        }

        return headRevList;
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = GetHeadRevList();

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        // Cuffing Head Revs is not enough - they must be killed.
        if (IsGroupDetainedOrDead(headRevList, false, false, false))
        {
            var rev = AllEntityQuery<RevolutionaryComponent, MindContainerComponent>();
            while (rev.MoveNext(out var uid, out _, out var mc))
            {
                if (HasComp<HeadRevolutionaryComponent>(uid))
                    continue;

                _npcFaction.RemoveFaction(uid, RevolutionaryNpcFaction);
                _stun.TryUpdateParalyzeDuration(uid, stunTime);
                RemCompDeferred<RevolutionaryComponent>(uid);
                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Entity(uid, EntityManager))), uid);
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

                if (!_mind.TryGetMind(uid, out var mindId, out var mind, mc))
                    continue;

                // remove their antag role
                _role.MindRemoveRole<RevolutionaryRoleComponent>(mindId);

                // make it very obvious to the rev they've been deconverted since
                // they may not see the popup due to antag and/or new player tunnel vision
                if (_player.TryGetSessionById(mind.UserId, out var session))
                    _euiMan.OpenEui(new DeconvertedEui(), session);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Will take a group of entities and check if these entities are alive, dead or cuffed.
    /// </summary>
    /// <param name="list">The list of the entities</param>
    /// <param name="checkOffStation">Bool for if you want to check if someone is in space and consider them missing in action. (Won't check when emergency shuttle arrives just in case)</param>
    /// <param name="countCuffed">Bool for if you don't want to count cuffed entities.</param>
    /// <param name="countRevolutionaries">Bool for if you want to count revolutionaries.</param>
    /// <returns></returns>
    private bool IsGroupDetainedOrDead(List<EntityUid> list, bool checkOffStation, bool countCuffed, bool countRevolutionaries)
    {
        var gone = 0;

        foreach (var entity in list)
        {
            if (TryComp<CuffableComponent>(entity, out var cuffed) && cuffed.CuffedHandCount > 0 && countCuffed)
            {
                gone++;
                continue;
            }

            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    gone++;
                    continue;
                }

                if (checkOffStation && _stationSystem.GetOwningStation(entity) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    gone++;
                    continue;
                }
            }
            //If they don't have the MobStateComponent they might as well be dead.
            else
            {
                gone++;
                continue;
            }

            if ((HasComp<RevolutionaryComponent>(entity) || HasComp<HeadRevolutionaryComponent>(entity)) && countRevolutionaries)
            {
                gone++;
                continue;
            }
        }

        return gone == list.Count || list.Count == 0;
    }

    /// <summary>
    /// Returns the percentage of a group that are either revolutionaries or head revolutionaries.
    /// </summary>
    /// <param name="list">The list to get the rev percentage of.</param>
    /// <returns></returns>
    private float GetRevolutionaryPercentage(List<EntityUid> list)
    {
        var revs = 0;
        foreach (var entity in list)
        {
            if (HasComp<RevolutionaryComponent>(entity) || HasComp<HeadRevolutionaryComponent>(entity))
                revs++;
        }

        if (list.Count == 0) return 0f;
        return revs / list.Count;
    }
}

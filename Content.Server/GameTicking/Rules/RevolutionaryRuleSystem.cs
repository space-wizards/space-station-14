using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Stunnable;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Shared.IdentityManagement;
using Content.Server.Flash;
using Content.Shared.Mindshield.Components;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Antag;
using Robust.Server.GameObjects;
using Content.Server.Speech.Components;
using Content.Server.Roles.Jobs;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelectionSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }

    /// <summary>
    /// Checks if the round should end and also checks who has a mindshield.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (GameTicker.IsGameRuleAdded(uid, gameRule) && component.CommandCheck <= _timing.CurTime)
        {
            component.CommandCheck = _timing.CurTime + component.TimerWait;
            CheckCommandLose();
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var revsLost = CheckRevsLose();
        var commandLost = CheckCommandLose();
        var query = AllEntityQuery<RevolutionaryRuleComponent>();
        while (query.MoveNext(out var headrev))
        {
            if (commandLost && !revsLost)
            {
                ev.AddLine(Loc.GetString("rev-won"));
            }
            else if (!commandLost && revsLost)
            {
                ev.AddLine(Loc.GetString("rev-lost"));
            }
            else if (commandLost && revsLost)
            {
                ev.AddLine(Loc.GetString("rev-stalemate"));
            }
            else if (!commandLost && !revsLost)
            {
                ev.AddLine(Loc.GetString("rev-reverse-stalemate"));
            }
            ev.AddLine(Loc.GetString("head-rev-initial-count", ("initialCount", headrev.HeadRevs.Count)));
            foreach (var player in headrev.HeadRevs)
            {
                ev.AddLine(Loc.GetString("head-rev-initial",
                    ("name", player.Key),
                    ("username", player.Value)));
            }
            break;
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            _antagSelectionSystem.AttemptStartGameRule(ev, uid, comp.MinPlayers, gameRule);
        }
    }

    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            InitialAssignCommandStaff();

            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            _antagSelectionSystem.EligiblePlayers(comp.RevPrototypeId, comp.MaxHeadRevs, comp.PlayersPerHeadRev, comp.HeadRevStartSound,
                "head-rev-role-greeting", "#5e9cff", out var chosen, false);
            GiveHeadRev(chosen, comp.RevPrototypeId, comp);
        }
    }

    private void GiveHeadRev(List<EntityUid> chosen, string antagProto, RevolutionaryRuleComponent? comp)
    {
        foreach (var headRev in chosen)
        {
            var inCharacterName = MetaData(headRev).EntityName;
            if (_mindSystem.TryGetMind(headRev, out var mindId, out var mind))
            {
                if (!_roleSystem.MindHasRole<RevolutionaryRoleComponent>(mindId))
                {
                    _roleSystem.MindAddRole(mindId, new RevolutionaryRoleComponent { PrototypeId = "Rev" });
                }
                if (_mindSystem.TryGetSession(mindId, out var session) && comp != null)
                {
                    comp.HeadRevs.Add(inCharacterName, session.Name);
                }
            }
            _antagSelectionSystem.GiveAntagBagGear(headRev, "RevFlash");
            _antagSelectionSystem.GiveAntagBagGear(headRev, "ClothingEyesGlassesSunglasses");
            EnsureComp<RevolutionaryComponent>(headRev);
            EnsureComp<HeadRevolutionaryComponent>(headRev);
        }
    }

    /// <summary>
    /// At start gets all the initial players to check for heads
    /// </summary>
    private void InitialAssignCommandStaff()
    {
        var playerList = new List<EntityUid>();
        var query = AllEntityQuery<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (HasComp<HumanoidAppearanceComponent>(uid))
            {
                playerList.Add(uid);
            }
        }
        AssignCommandStaff(playerList);
    }

    /// <summary>
    /// Gives heads the command component so they are tracked for Revs.
    /// </summary>
    /// <param name="playerList"></param>
    private void AssignCommandStaff(List<EntityUid> playerList, bool latejoin = false)
    {
        var currentCommandStaff = 0;
        var jobs = _prototypeManager.Index<DepartmentPrototype>("Command");
        foreach (var player in playerList)
        {
            if (_mindSystem.TryGetMind(player, out var mindId, out var mind))
            {
                if (_jobSystem.MindTryGetJob(mindId, out var jobComp, out var jobProto) && jobs.Roles.Contains(jobProto.ID) && mind.OriginalOwnedEntity != null)
                {
                    if (!HasComp<HeadRevolutionaryComponent>(mind.OriginalOwnedEntity))
                    {
                        EnsureComp<CommandStaffComponent>(mind.OriginalOwnedEntity.Value);
                        currentCommandStaff++;
                    }
                }
            }
        }
        if (latejoin == false)
        {
            if (currentCommandStaff == 0)
            {
                var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
                while (query.MoveNext(out var uid, out var comp, out var gameRule))
                {
                    GameTicker.EndGameRule(uid, gameRule);
                }
            }
        }
    }

    /// <summary>
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    public void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        var stunTime = TimeSpan.FromSeconds(3);
        if (!HasComp<RevolutionaryComponent>(ev.Target) && !HasComp<MindShieldComponent>(ev.Target) && (HasComp<HumanoidAppearanceComponent>(ev.Target) || HasComp<MonkeyAccentComponent>(ev.Target))
            && TryComp<MobStateComponent>(ev.Target, out var mobState) && mobState.CurrentState == MobState.Alive)
        {
            _npcFaction.AddFaction(ev.Target, "Revolutionary");
            EnsureComp<RevolutionaryComponent>(ev.Target);
            _sharedStun.TryParalyze(ev.Target, stunTime, true);
            if (ev.User != null)
            {
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");
            }
            if (_mindSystem.TryGetMind(ev.Target, out var mindId, out var mind))
            {
                if (!_roleSystem.MindHasRole<RevolutionaryRoleComponent>(mindId))
                {
                    _roleSystem.MindAddRole(mindId, new RevolutionaryRoleComponent { PrototypeId = "Rev" });
                }
                if (mind.Session != null)
                {
                    var message = Loc.GetString("rev-role-greeting");
                    var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                    _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Red);
                }
            }
        }
    }

    public void OnHeadRevAdmin(EntityUid mindId, MindComponent mind)
    {
        var revRule = EntityQuery<RevolutionaryRuleComponent>().FirstOrDefault();
        if (revRule == null)
        {
            GameTicker.StartGameRule("Revolutionary");
        }
        if (mind.OwnedEntity != null)
        {
            var player = new List<EntityUid>();
            player.Add((EntityUid) mind.OwnedEntity);
            InitialAssignCommandStaff();
            GiveHeadRev(player, "Rev", null);
        }
        if (mind.Session != null)
        {
            var message = Loc.GetString("head-rev-role-greeting");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.FromHex("#5e9cff"));
        }
    }
    private void OnCommandMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose();
    }
    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    private bool CheckCommandLose()
    {
        var commandLost = false;
        var headJobs = _prototypeManager.Index<DepartmentPrototype>("Command");
        var secJobs = _prototypeManager.Index<DepartmentPrototype>("Security");
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent, MobStateComponent>();
        while (heads.MoveNext(out var id, out var command, out var state))
        {
            commandList.Add(id);
            if (command.HeadsDied == true)
            {
                commandLost = true;
            }
        }

        if (_antagSelectionSystem.IsGroupDead(commandList, true))
        {
            if (commandLost == false)
            {
                foreach (var command in commandList)
                {
                    if (TryComp<CommandStaffComponent>(command, out var comp))
                    {
                        comp.HeadsDied = true;
                    }
                }
                foreach (var station in _stationSystem.GetStations())
                {
                    var jobs = _stationJobs.GetJobs(station).Keys.ToList();
                    _chat.DispatchStationAnnouncement(station, Loc.GetString("rev-all-heads-dead"), "Revolutionary", colorOverride: Color.FromHex("#5e9cff"));
                    foreach (var job in jobs)
                    {
                        var currentJob = job.Replace(" ", "");
                        if (headJobs.Roles.Contains(currentJob) || secJobs.Roles.Contains(currentJob))
                        {
                            _stationJobs.TrySetJobSlot(station, job, 0);
                        }
                    }
                }
            }
            return true;
        }
        else return false;
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose();
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var id, out var comp, out var state))
            headRevList.Add(id);

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        if (_antagSelectionSystem.IsGroupDead(headRevList, false))
        {
            var rev = AllEntityQuery<RevolutionaryComponent>();
            while (rev.MoveNext(out var id, out var comp))
            {
                if (HasComp<RevolutionaryComponent>(id) && !HasComp<HeadRevolutionaryComponent>(id))
                {
                    var name = Identity.Entity(id, EntityManager);
                    _npcFaction.RemoveFaction(id, "Revolutionary");
                    _sharedStun.TryParalyze(id, stunTime, true);
                    RemCompDeferred<RevolutionaryComponent>(id);
                    _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), id);
                    _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(id)} was deconverted due to all Head Revolutionaries dying.");
                }
            }
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Gives late join heads the head component so they also need to be killed.
    /// </summary>
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_mindSystem.TryGetMind(ev.Player, out var mindId, out var mind) && HasComp<PendingClockInComponent>(mind.OwnedEntity) && ev.Player.AttachedEntity != null)
        {
            var list = new List<EntityUid>();
            list.Add((EntityUid) ev.Player.AttachedEntity);
            AssignCommandStaff(list, true);
        }
    }

}

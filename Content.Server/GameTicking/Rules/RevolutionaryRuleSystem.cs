using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Stunnable;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.IdentityManagement;
using Content.Server.Flash;
using Content.Shared.Mindshield.Components;
using Content.Shared.Charges.Systems;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Antag;
using Robust.Server.GameObjects;
using Content.Server.Speech.Components;
using Content.Server.Mind.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelectionSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private TimeSpan _timerWait = TimeSpan.FromSeconds(20);
    private TimeSpan _endRoundCheck = default!;
    private bool _headsDied = false;
    private bool _revsLost = false;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _endRoundCheck = _timing.CurTime + _timerWait;
    }

    /// <summary>
    /// Checks if the round should end and also checks who has a mindshield.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (GameTicker.IsGameRuleAdded(uid, gameRule) && _endRoundCheck <= _timing.CurTime)
        {
            _endRoundCheck = _timing.CurTime + _timerWait;
            CheckFinish();
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent>();
        while (query.MoveNext(out var headrev))
        {
            if (_headsDied && !_revsLost)
            {
                ev.AddLine(Loc.GetString("rev-won"));
            }
            if (!_headsDied && _revsLost)
            {
                ev.AddLine(Loc.GetString("rev-lost"));
            }
            if (!_headsDied && !_revsLost)
            {
                ev.AddLine(Loc.GetString("rev-stalemate"));
            }
            if (_headsDied && _revsLost)
            {
                ev.AddLine(Loc.GetString("rev-reversestalemate"));
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
            continue;
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
            continue;
        }
    }

    private void GiveHeadRev(List<EntityUid> chosen, string antagProto, RevolutionaryRuleComponent? comp)
    {
        foreach (var headRev in chosen)
        {
            var inCharacterName = MetaData(headRev).EntityName;
            var mind = _mindSystem.GetMind(headRev);
            _antagSelectionSystem.GiveAntagBagGear(headRev, "Flash");
            _antagSelectionSystem.GiveAntagBagGear(headRev, "ClothingEyesGlassesSunglasses");
            EnsureComp<RevolutionaryComponent>(headRev);
            EnsureComp<HeadRevolutionaryComponent>(headRev);
            if (mind != null)
            {
                _mindSystem.AddRole(mind, new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>(antagProto)));
                if (comp != null)
                {
                    if (_mindSystem.TryGetSession(mind, out var session))
                    {
                        comp.HeadRevs.Add(inCharacterName, session.Name);
                    }
                }
            }
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
            var mind = _mindSystem.GetMind(player);
            if (mind != null && mind.CurrentJob != null)
            {
                var currentJob = mind.CurrentJob.Name;
                currentJob = currentJob.Replace(" ", "");
                if (mind.OwnedEntity != null && jobs.Roles.Contains(currentJob))
                {
                    if (!HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
                    {
                        EnsureComp<CommandStaffComponent>(mind.OwnedEntity.Value);
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
            if (ev.Used != null)
            {
                _charges.AddCharges(ev.Used.Value, 1);
            }
            _npcFaction.AddFaction(ev.Target, "Revolutionary");
            EnsureComp<RevolutionaryComponent>(ev.Target);
            _sharedStun.TryParalyze(ev.Target, stunTime, true);
            if (ev.User != null)
            {
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");
            }
            if (_mindSystem.TryGetMind(ev.Target, out var mind))
            {
                _mindSystem.AddRole(mind, new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>("Rev")));
                if (mind.Session != null)
                {
                    var message = Loc.GetString("rev-role-greeting");
                    var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                    _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Red);
                }
            }
        }
    }

    public void OnHeadRevAdmin(Mind.Mind mind, IPlayerSession headRev)
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
    private void OnMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckFinish();
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckFinish();
    }

    /// <summary>
    /// Checks if all Head Revs are dead and if all command is dead to either end the round or remove all revs. Or both.
    /// </summary>
    private void CheckFinish()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headJobs = _prototypeManager.Index<DepartmentPrototype>("Command");
        var secJobs = _prototypeManager.Index<DepartmentPrototype>("Security");

        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revs, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
            var inRound = 0;
            var dead = 0;
            while (headRevs.MoveNext(out var id, out var comp, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                inRound++;
            }

            // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
            if (dead == inRound)
            {
                _revsLost = true;
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
            }
            else
            {
                _revsLost = false;
            }
            // Checks if all heads are dead to finish the round.
            var heads = AllEntityQuery<CommandStaffComponent, MobStateComponent>();
            inRound = 0;
            dead = 0;

            while (heads.MoveNext(out var id, out var command, out var state))
            {
                if (state.CurrentState == MobState.Dead || state.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                else if (_stationSystem.GetOwningStation(id) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    dead++;
                }
                inRound++;
            }

            //In the rare instances that no heads are on station at start, I put a timer before this can activate. Might lower it
            //Also now should set all command and sec jobs to zero.
            if (dead == inRound && _headsDied)
            {
                _headsDied = true;
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

        }
    } 

    /// <summary>
    /// Gives late join heads the head component so they also need to be killed.
    /// </summary>
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        var mind = ev.Player.GetMind();
        if (mind != null && HasComp<PendingClockInComponent>(mind.OwnedEntity) && ev.Player.AttachedEntity != null)
        {
            var list = new List<EntityUid>();
            list.Add((EntityUid) ev.Player.AttachedEntity);
            AssignCommandStaff(list, true);
        }
    }
}

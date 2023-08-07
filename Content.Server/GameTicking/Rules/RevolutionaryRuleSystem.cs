using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Stunnable;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Server.Mindshield;
using Content.Server.Shuttles.Systems;
using Content.Server.Objectives.Interfaces;
using Content.Shared.IdentityManagement;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for game the game to end.)
/// </summary>

public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MindShieldSystem _mindShield = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private TimeSpan _timerWait = TimeSpan.FromSeconds(10);

    private TimeSpan _endRoundCheck = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<RevolutionaryRuleComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _endRoundCheck = _timing.CurTime + _timerWait;
    }

    /// <summary>
    /// Checks if any command staff are in space and basically count them as dead. Also checks for round end and if someone has an implanted mindshield.
    /// </summary>

    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (GameTicker.IsGameRuleAdded(uid, gameRule) && _endRoundCheck <= _timing.CurTime)
        {
            var headsOffStation = AllEntityQuery<CommandComponent>();
            while (headsOffStation.MoveNext(out var id, out var comp))
            {
                if (_stationSystem.GetOwningStation(id) == null && !_emergencyShuttle.EmergencyShuttleArrived)
                {
                    EnsureComp<CheckForHeadsInSpaceComponent>(id);
                }
                else if (HasComp<CheckForHeadsInSpaceComponent>(id))
                {
                    RemComp<CheckForHeadsInSpaceComponent>(id);
                }
            }
            _endRoundCheck = _timing.CurTime + _timerWait;
            _mindShield.MindShieldCheck();
            CheckFinish();
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {

        foreach (var headrev in EntityQuery<RevolutionaryRuleComponent>())
        {
            if (headrev.HeadsDied && !headrev.RevsLost)
            {
                ev.AddLine(Loc.GetString("rev-won"));
            }
            if (!headrev.HeadsDied && headrev.RevsLost)
            {
                ev.AddLine(Loc.GetString("rev-lost"));
            }
            if (!headrev.HeadsDied && !headrev.RevsLost)
            {
                ev.AddLine(Loc.GetString("rev-stalemate"));
            }
            ev.AddLine(Loc.GetString("head-rev-initial-count", ("initialCount", headrev.HeadRevs.Count)));
            foreach (var player in headrev.HeadRevs)
            {
                ev.AddLine(Loc.GetString("head-rev-initial",
                    ("name", player.Key),
                    ("username", player.Value)));
            }
        }
    }
    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revs, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            if (!ev.Forced && ev.Players.Length < revs.MinPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("rev-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", revs.MinPlayers)));
                ev.Cancel();
                continue;
            }
            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rev-no-one-ready"));
                ev.Cancel();
            }
        }
    }
    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            AssignHeadRevs(comp);
            break;
        }
    }
    /// <summary>
    /// Gets the list of players currently spawned in and checks if they are eligible to become a Head Rev.
    /// </summary>

    private void AssignHeadRevs(RevolutionaryRuleComponent comp)
    {
        var allPlayers = _playerSystem.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            var mind = player.GetMind();
            if (!player.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false)
                continue;

            if (player.AttachedEntity == null || HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
                playerList.Add(player);
            else
                continue;

            var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(comp.HeadRevPrototypeId))
                prefList.Add(player);
        }
        if (playerList.Count == 0)
            return;

        AssignCommandStaff();
        var headRevs = Math.Clamp(allPlayers.Count / comp.PlayersPerHeadRev, 1, comp.MaxHeadRevs);
        for (var revs = 0; revs < headRevs; revs++)
        {
            IPlayerSession headRev;
            if (prefList.Count == 0)
            {
                if (playerList.Count == 0)
                {
                    break;
                }
                headRev = _random.PickAndTake(playerList);
            }
            else
            {
                headRev = _random.PickAndTake(prefList);
                playerList.Remove(headRev);
            }
            var mind = headRev.Data.ContentData()?.Mind;
            if (mind != null && mind.OwnedEntity != null)
            {
                EnsureComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
                GiveHeadRevRole(mind, headRev);
            }
        }
    }
    /// <summary>
    /// Gets player chosen to become Head Rev from previous method and gives them the role and gear.
    /// </summary>

    private void GiveHeadRevRole(Mind.Mind mind, IPlayerSession headRev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            _mindSystem.AddRole(mind, new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>(comp.HeadRevPrototypeId)));
            var inCharacterName = string.Empty;
            if (mind.OwnedEntity != null)
            {
                if (HasComp<CommandComponent>(mind.OwnedEntity.Value))
                {
                    RemComp<CommandComponent>(mind.OwnedEntity.Value);
                }
                AddComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value);
                AddComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>(comp.HeadRevGearPrototypeId), null);
                _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "NanoTrasen", false);
                _npcFaction.AddFaction(mind.OwnedEntity.Value, "Revolutionary");
                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;
                comp.HeadRevs.Add(inCharacterName, headRev.Name);
                _audioSystem.PlayGlobal(comp.HeadRevStartSound, mind.OwnedEntity.Value);
            }
            if (mind.Session != null)
            {
                var message = Loc.GetString("head-rev-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.FromHex("#5e9cff"));
            }
        }
    }

    /// <summary>
    /// Assigns command staff on station with a component so they can be used later.
    /// </summary>

    private void AssignCommandStaff()
    {
        var jobs = _prototypeManager.Index<DepartmentPrototype>("Command");
        var allPlayers = _playerSystem.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            if (player.AttachedEntity == null || HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
            {
                playerList.Add(player);
            }
        }
        foreach (var player in playerList)
        {
            var mind = player.GetMind();
            if (mind != null && mind.CurrentJob != null)
            {
                var currentJob = mind.CurrentJob.Name;
                currentJob = currentJob.Replace(" ", "");
                if (mind.OwnedEntity != null && jobs.Roles.Contains(currentJob))
                {
                    if (!HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
                    {
                        EnsureComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
                        EnsureComp<CommandComponent>(mind.OwnedEntity.Value);
                    }
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
            EnsureComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
            GiveHeadRevRole(mind, headRev);
            AssignCommandStaff();
        }
    }
    private void OnMobStateChanged(EntityUid uid, RevolutionaryRuleComponent comp, MobStateChangedEvent ev)
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
            var headRevs = EntityQuery<HeadRevolutionaryComponent, MobStateComponent>(true);
            var inRound = 0;
            var dead = 0;
            foreach (var rev in headRevs)
            {
                if (rev.Item2.CurrentState == MobState.Dead || rev.Item2.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                inRound++;
            }

            // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
            if (dead == inRound)
            {
                revs.RevsLost = true;
                var allPlayers = _playerSystem.ServerSessions.ToList();
                foreach (var rev in allPlayers)
                {
                    var mind = rev.GetMind();
                    if (mind != null)
                        if (HasComp<RevolutionaryComponent>(mind.OwnedEntity) && !HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
                        {
                            var name = Identity.Entity(mind.OwnedEntity.Value, EntityManager);
                            _npcFaction.AddFaction(mind.OwnedEntity.Value, "NanoTrasen");
                            _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "Revolutionary");
                            _sharedStun.TryParalyze(mind.OwnedEntity.Value, stunTime, true);
                            RemComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                            RemComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
                            _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), mind.OwnedEntity.Value);

                        }
                }
            }
            // Checks if all heads are dead to finish the round.

            var heads = EntityQuery<CommandComponent, MobStateComponent>(true);
            var headsOffStation = EntityQuery<CommandComponent, CheckForHeadsInSpaceComponent, MobStateComponent>();
            inRound = 0;
            dead = 0;
            foreach (var head in heads)
            {
                if (head.Item2.CurrentState == MobState.Dead || head.Item2.CurrentState == MobState.Invalid)
                {
                    dead++;
                }
                inRound++;
            }
            foreach (var head in headsOffStation)
            {
                if (head.Item3.CurrentState != MobState.Dead && head.Item3.CurrentState != MobState.Invalid)
                {
                    dead++;
                }
            }

            //In the rare instances that no heads are on station at start, I put a timer before this can activate. Might lower it
            //Also now should set all command and sec jobs to zero.

            if (dead == inRound && !revs.HeadsDied && _timing.CurTime >= TimeSpan.FromMinutes(revs.GracePeriod))
            {
                revs.HeadsDied = true;
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
        if (mind != null && HasComp<PendingClockInComponent>(mind.OwnedEntity) && mind.CurrentJob != null)
        {
            AssignCommandStaff();
        }
    }
}

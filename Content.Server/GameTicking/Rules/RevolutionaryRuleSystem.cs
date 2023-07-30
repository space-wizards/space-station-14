using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Revolutionary;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Shared.Revolutionary;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Stunnable;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;

    private ISawmill _sawmill = default!;

    private int MinPlayers => _cfg.GetCVar(CCVars.RevolutionaryMinPlayers);
    private int MaxRevHeads => _cfg.GetCVar(CCVars.RevolutionaryMaxHeadRevs);
    private int HeadRevsPerPlayer => _cfg.GetCVar(CCVars.RevolutionaryPlayersPerHeadRev);

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("preset");
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
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
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revs, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            if (!ev.Forced || ev.Players.Length < MinPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("rev-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length),
                    ("minimumPlayers", MinPlayers)));
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
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            AssignHeadRevs(comp);
            break;
        }
    }
    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }
    private void AssignHeadRevs(RevolutionaryRuleComponent comp)
    {
        var allPlayers = _playerSystem.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();
        var cantBeAntag = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            var mind = player.GetMind();
            if (!player.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false)
            {
                cantBeAntag.Add(player);
                continue;
            }
            if (player.AttachedEntity == null || HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
            {
                playerList.Add(player);
            }
            else
                continue;

            var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(player.UserId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(comp.HeadRevPrototypeId))
                prefList.Add(player);
        }
        if (playerList.Count == 0)
            return;

        _sawmill.Error("Math");
        var headRevs = Math.Clamp(allPlayers.Count / HeadRevsPerPlayer, 1, MaxRevHeads);
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
            if (mind == null)
            {
                continue;
            }
            _mindSystem.AddRole(mind, new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>(comp.HeadRevPrototypeId)));
            var inCharacterName = string.Empty;
            if (mind.OwnedEntity != null)
            {
                AddComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
                AddComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value);
                AddComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>(comp.HeadRevGearPrototypeId), null);
                _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "NanoTrasen", false);
                _npcFaction.AddFaction(mind.OwnedEntity.Value, "Revolutionary");
                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;
                comp.HeadRevs.Add(inCharacterName, headRev.Name);
            }
            if (mind.Session != null)
            {
                var message = Loc.GetString("head-rev-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.MediumBlue);
            }
        }
        AssignCommandStaff(cantBeAntag);
    }

    //Assigns command staff based on their weight (real). I THINK all head weight is above 10 so uh let me know if not.
    //I wanted to do sec as well but I'm not sure if they have a specific weight and they have glasses so if they get converted it's their own fault.
    private void AssignCommandStaff(List<IPlayerSession> players)
    {
        foreach (var player in players)
        {
            var mind = player.GetMind();
            if (mind != null && mind.CurrentJob != null)
            {
                if (mind.OwnedEntity != null && mind.CurrentJob.Prototype.Weight >= 10)
                {
                    AddComp<HeadComponent>(mind.OwnedEntity.Value);
                }
            }
        }
    }


    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckRevsLose(ev.Target);
    }

    private void CheckRevsLose(EntityUid target)
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revs, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var headRevs = EntityQuery<HeadRevolutionaryComponent, MobStateComponent>(true);
            var aliveHeadRevs = headRevs
                .Any(ent => ent.Item2.CurrentState != MobState.Dead && ent.Item1.Running);
            //If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
            if (!aliveHeadRevs)
            {
                revs.RevsLost = true;
                var allPlayers = _playerSystem.ServerSessions.ToList();
                foreach (var rev in allPlayers)
                {
                    var mind = rev.GetMind();
                    if (mind != null)
                        if (HasComp<RevolutionaryComponent>(mind.OwnedEntity) && !HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
                        {
                            _npcFaction.AddFaction(mind.OwnedEntity.Value, "NanoTrasen");
                            _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "Revolutionary");
                            _sharedStun.TryParalyze(mind.OwnedEntity.Value, stunTime, true);
                            RemComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                        }
                }
            }
            CheckRevsWin();
        }
    }
    //I LOVE REUSING CODE DIRECTLY ABOVE
    private void CheckRevsWin()
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revs, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var heads = EntityQuery<HeadComponent, MobStateComponent>(true);
            var aliveHeads = heads
                .Any(ent => ent.Item2.CurrentState != MobState.Dead && ent.Item1.Running);
            //In the rare instances that no heads are on station at start, I put a timer before this can activate. Might lower it
            if (!aliveHeads && revs.ShuttleCalled == false && _timing.CurTime >= TimeSpan.FromMinutes(5))
            {
                revs.HeadsDied = true;
                foreach (var station in _stationSystem.GetStations())
                {
                    _chat.DispatchStationAnnouncement(station, Loc.GetString("rev-shuttle-call"), colorOverride: Color.MediumBlue);
                }
                _roundEndSystem.RequestRoundEnd(null, false);
                revs.ShuttleCalled = true;
            }

        }
    }
    //Should give late join command the head component. Should.
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        var mind = ev.Player.GetMind();
        if (mind != null && HasComp<PendingClockInComponent>(mind.OwnedEntity) && mind.CurrentJob != null)
        {
            if (mind.CurrentJob.Prototype.Weight >= 10)
            {
                AddComp<HeadComponent>(mind.OwnedEntity.Value);
                _sawmill.Error("Late join command added");
            }
        }
    }

    // Admin command
    public void MakeHeadRev(Mind.Mind mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return;
        _mindSystem.AddRole(mind, new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>("HeadRev")));
        _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>("HeadRevGear"), null);
        _npcFaction.RemoveFaction(mind.OwnedEntity.Value, "NanoTrasen");
        _npcFaction.AddFaction(mind.OwnedEntity.Value, "Revolutionary");
        GameTicker.AddGameRule("Revolutionary");
        EnsureComp<RevolutionaryRuleComponent>(mind.OwnedEntity.Value);
        EnsureComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value);
        EnsureComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
        if (mind.Session != null)
        {
            var message = Loc.GetString("head-rev-role-greeting");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.MediumBlue);
        }
        AssignCommandStaffAdmin();
    }
    // Assigns command staff when forcing an admin Head Rev.
    private void AssignCommandStaffAdmin()
    {
        var allPlayers = _playerSystem.ServerSessions.ToList();
        foreach (var player in allPlayers)
        {
            var mind = player.GetMind();
            if (mind != null && HasComp<HumanoidAppearanceComponent>(mind.OwnedEntity) && mind.CurrentJob != null && !HasComp<RevolutionaryComponent>(mind.OwnedEntity))
            {
                if (mind.CurrentJob.Prototype.Weight >= 10)
                {
                    AddComp<HeadComponent>(mind.OwnedEntity.Value);
                }
            }
        }
    }
}

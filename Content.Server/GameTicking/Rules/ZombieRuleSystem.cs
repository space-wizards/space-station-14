using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Disease;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Traitor;
using Content.Server.Zombies;
using Content.Shared.CCVar;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.FixedPoint;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class ZombieRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly NukeSystem _nukeSystem = default!;
    [Dependency] private readonly NukeCodeSystem _nukeCodeSystem = default!;

    private Dictionary<string, string> _initialInfected = new();

    public override string Prototype => "Zombie";
    private const string PatientZeroPrototypeID = "PatientZero";
    private const string InitialZombieVirusPrototype = "ActiveZombieVirus";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        //SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);

        SubscribeLocalEvent<EntityZombifiedEvent>(OnEntityZombified);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!Enabled)
            return;

        //it's my if-else chain and no one can tell me how to write it!
        var percent = GetInfectedPercentage();
        Logger.Debug(percent.ToString());
        if (percent <= 0)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-none"));
        else if (percent <= 0.25)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-low"));
        else if (percent <= 0.5)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-medium", ("percent", (percent * 100).ToString())));
        else if (percent < 1)
            ev.AddLine(Loc.GetString("zombie-round-end-amount-high", ("percent", (percent * 100).ToString())));
        else
            ev.AddLine(Loc.GetString("zombie-round-end-amount-all"));

        ev.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", _initialInfected.Count)));
        foreach (var player in _initialInfected)
        {
            ev.AddLine(Loc.GetString("zombie-round-end-user-was-initial",
                ("name", player.Key),
                ("username", player.Value)));
        }
    }

    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!Enabled)
            return;

        _initialInfected = new();

        InfectInitialPlayers();
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!Enabled)
            return;
    }

    private void OnEntityZombified(EntityZombifiedEvent ev)
    {
        if (!Enabled)
            return;

        //we only care about players, not monkeys and such.
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target))
            return;

        var percent = GetInfectedPercentage();

        if (percent >= 1)
            _roundEndSystem.EndRound();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!Enabled)
            return;

        //Uncomment this once im done local testing
        /*
        var minPlayers = _cfg.GetCVar(CCVars.ZombieMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("zombie-no-one-ready"));
            ev.Cancel();
            return;
        }
        //*/
    }

    public override void Started(GameRuleConfiguration configuration)
    {
        //this technically will run twice with zombies on roundstart, but it doesn't matter because it fails instantly
        InfectInitialPlayers();
    }

    public override void Ended(GameRuleConfiguration configuration) { }

    private FixedPoint2 GetInfectedPercentage()
    {
        var allPlayers = EntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        var totalPlayers = new List<EntityUid>();
        var livingZombies = new List<EntityUid>();
        foreach (var ent in allPlayers)
        {
            if (ent.Item2.IsAlive())
            {
                totalPlayers.Add(ent.Item2.Owner);

                if (HasComp<ZombieComponent>(ent.Item1.Owner))
                    livingZombies.Add(ent.Item2.Owner);
            }    
        }
        Logger.Debug((livingZombies.Count / totalPlayers.Count).ToString());
        return (FixedPoint2) livingZombies.Count / (FixedPoint2) totalPlayers.Count;
    }

    /// <summary>
    ///     Infects the first players with the passive zombie virus.
    ///     Also records their names for the end of round screen.
    /// </summary>
    private void InfectInitialPlayers()
    {
        var allPlayers = _playerManager.ServerSessions.ToList();
        var playerList = new List<IPlayerSession>();
        foreach (var player in allPlayers)
        {
            if (player.AttachedEntity != null)
            {
                playerList.Add(player);
            }
        }

        if (playerList.Count == 0)
            return;

        var playersPerInfected = _cfg.GetCVar(CCVars.ZombiePlayersPerInfected);
        var maxInfected = _cfg.GetCVar(CCVars.ZombieMaxInfected);

        var numInfected = Math.Max(1,
            (int) Math.Min(
                Math.Floor((double) playerList.Count / playersPerInfected), maxInfected));

        for (var i = 0; i < numInfected; i++)
        {
            if (playerList.Count == 0)
            {
                Logger.InfoS("preset", "Insufficient number of players. stopping selection.");
                break;
            }
            var zombie = _random.PickAndTake(playerList);
            playerList.Remove(zombie);
            Logger.InfoS("preset", "Selected a patient 0.");

            var mind = zombie.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked patient 0.");
                continue;
            }

            DebugTools.AssertNotNull(mind.OwnedEntity);

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(PatientZeroPrototypeID)));
            var inCharacterName = string.Empty;
            if (mind.OwnedEntity != null)
            {
                _diseaseSystem.TryAddDisease(mind.OwnedEntity.Value, InitialZombieVirusPrototype);
                inCharacterName = MetaData(mind.OwnedEntity.Value).EntityName;
            }

            if (mind.Session != null)
            {
                var messageWrapper = Loc.GetString("chat-manager-server-wrap-message");

                //gets the names now in case the players leave.
                if (inCharacterName != null)
                    _initialInfected.Add(inCharacterName, mind.Session.Name);

                // I went all the way to ChatManager.cs and all i got was this lousy T-shirt
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, Loc.GetString("zombie-patientzero-role-greeting"),
                   messageWrapper, default, false, mind.Session.ConnectedClient, Color.Plum);
            }
        }
    }
}

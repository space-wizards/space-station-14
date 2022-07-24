using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Server.Traitor;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.MobState;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    private Dictionary<Mind.Mind, bool> _aliveNukeops = new();
    private bool _opsWon;

    public override string Prototype => "Nukeops";

    private const string NukeopsPrototypeId = "Nukeops";
    private const string NukeopsCommanderPrototypeId = "NukeopsCommander";

    private EntityUid? _outpost;
    private List<EntityCoordinates> _spawns = new();
    private List<IPlayerSession> _prefList = new ();
    private List<IPlayerSession> _cmdrPrefList = new();
    private List<IPlayerSession> _operatives = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
    	if (!RuleAdded)
            return;

        _opsWon = true;
        _roundEndSystem.EndRound();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        ev.AddLine(_opsWon ? Loc.GetString("nukeops-ops-won") : Loc.GetString("nukeops-crew-won"));
        ev.AddLine(Loc.GetString("nukeops-list-start"));
        foreach (var nukeop in _aliveNukeops)
        {
            ev.AddLine($"- {nukeop.Key.Session?.Name}");
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded)
            return;

        if (!_aliveNukeops.TryFirstOrNull(x => x.Key.OwnedEntity == ev.Entity, out var op)) return;

        _aliveNukeops[op.Value.Key] = op.Value.Key.CharacterDeadIC;

        if (_aliveNukeops.Values.All(x => !x))
        {
            _roundEndSystem.EndRound();
        }
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!RuleAdded)
            return;

        _aliveNukeops.Clear();
        FindValidNukeOpPlayers(ev.PlayerPool);
        PickNukies(ev.PlayerPool);
        CreateNukeOpShip();
        if (!_outpost.HasValue)
        {
            Logger.WarningS("nukies", $"Failed to create the NukeOp ship");
            return;
        }

        FindSpawns();

        CreateRoundStartNukies(ev);
    }

    public void FindSpawns()
    {
        _spawns = new List<EntityCoordinates>();

        if (!_outpost.HasValue)
        {
            Logger.WarningS("nukies", $"Output was not created, cannot find spawn points for NukeOps.");
            return;
        }

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != "SpawnPointNukies") continue;

            if (xform.ParentUid == _outpost)
            {
                _spawns.Add(xform.Coordinates);
                break;
            }
        }

        if (_spawns.Count == 0)
        {
            _spawns.Add(EntityManager.GetComponent<TransformComponent>(_outpost.Value).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }
    }

    public void CreateRoundStartNukies(RulePlayerSpawningEvent ev)
    {
        // TODO: Loot table or something
        var commanderGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateCommanderGearFull");
        var starterGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull");
        var medicGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeMedicFull");
        var syndicateNamesElite = new List<string>(_prototypeManager.Index<DatasetPrototype>("SyndicateNamesElite").Values);
        var syndicateNamesNormal = new List<string>(_prototypeManager.Index<DatasetPrototype>("SyndicateNamesNormal").Values);

        // TODO: This should spawn the nukies in regardless and transfer if possible; rest should go to shot roles.
        for (var i = 0; i < _operatives.Count; i++)
        {
            string name;
            string role;
            StartingGearPrototype gear;

            switch (i)
            {
                case 0:
                    name = $"Commander " + _random.PickAndTake<string>(syndicateNamesElite);
                    role = NukeopsCommanderPrototypeId;
                    gear = commanderGear;
                    break;
                case 1:
                    name = $"Agent " + _random.PickAndTake<string>(syndicateNamesNormal);
                    role = NukeopsPrototypeId;
                    gear = medicGear;
                    break;
                default:
                    name = $"Operator " + _random.PickAndTake<string>(syndicateNamesNormal);
                    role = NukeopsPrototypeId;
                    gear = starterGear;
                    break;
            }

            var session = _operatives[i];
            MakeNukeOpOnShip(session, name, role, gear, ev.PlayerPool);
        }
    }

    //This does NOT take the round-start event, so that it could be used mid-round with a list of ghosts.
    public void FindValidNukeOpPlayers(List<IPlayerSession> players)
    {
        _operatives.Clear();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in players) //everyone
        {
            if (player.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false) continue;

            //Pull the profile here to simplify the parameters being passed in.
            var prefs = _preferencesManager.GetPreferences(player.UserId);
            var profile = prefs.SelectedCharacter as HumanoidCharacterProfile;
            if (profile == null)
                continue;

            if (profile.AntagPreferences.Contains(NukeopsPrototypeId))
            {
                _prefList.Add(player);
            }
            if (profile.AntagPreferences.Contains(NukeopsCommanderPrototypeId))
            {
                _cmdrPrefList.Add(player);
            }
        }
    }

    public void PickNukies(List<IPlayerSession> players)
    {
        if (players.Count == 0)
        {
            Logger.InfoS("preset", "Insufficient ready players to fill up with nukeops, stopping the selection");
            return;
        }

        var playersPerOperative = _cfg.GetCVar(CCVars.NukeopsPlayersPerOp);
        var maxOperatives = _cfg.GetCVar(CCVars.NukeopsMaxOps);

        var numNukies = MathHelper.Clamp(players.Count / playersPerOperative, 1, maxOperatives);
        var remainingPlayers = players.ToList();

        for (var i = 0; i < numNukies; i++)
        {
            IPlayerSession nukeOp;
            // Commander is simply the first entry in the list of operatives.
            if (_cmdrPrefList.Count > 0)
            {
                nukeOp = _random.PickAndTake(_cmdrPrefList);
                remainingPlayers.Remove(nukeOp);
                _cmdrPrefList.Clear(); //No longer relevant, wipe the list.
                Logger.InfoS("preset", "Selected a preferred nukeop commander.");
            }
            else if (_prefList.Count > 0)
            {
                nukeOp = _random.PickAndTake(_prefList);
                remainingPlayers.Remove(nukeOp);
                Logger.InfoS("preset", "Selected a preferred nukeop.");
            }
            else if (remainingPlayers.Count > 0)
            {
                nukeOp = _random.PickAndTake(remainingPlayers);
                Logger.InfoS("preset", "Insufficient preferred nukeop players, picking at random.");
            }
            else
            {
                Logger.InfoS("preset", "Insufficient ready players to reach max operative count, stopping the selection early");
                break;
            }

            _operatives.Add(nukeOp);
        }
    }

    public void MakeNukeOpOnShip(IPlayerSession session, string name, string role, StartingGearPrototype gear, List<IPlayerSession> playerPool)
    {
        var newMind = new Mind.Mind(session.UserId)
        {
            CharacterName = name
        };
        newMind.ChangeOwningPlayer(session.UserId);
        newMind.AddRole(new TraitorRole(newMind, _prototypeManager.Index<AntagPrototype>(role)));

        var mob = EntityManager.SpawnEntity("MobHuman", _random.Pick(_spawns));
        EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;
        EntityManager.AddComponent<RandomHumanoidAppearanceComponent>(mob);

        newMind.TransferTo(mob);
        _stationSpawningSystem.EquipStartingGear(mob, gear, null);

        playerPool.Remove(session);
        _aliveNukeops.Add(newMind, true);

        GameTicker.PlayerJoinGame(session);
    }

    public void CreateNukeOpShip()
    {
        // TODO: Make this a prototype
        // so true PAUL!
        var path = "/Maps/nukieplanet.yml";
        var shuttlePath = "/Maps/infiltrator.yml";
        var mapId = _mapManager.CreateMap();

        var (_, loadedOutpost) = _mapLoader.LoadBlueprint(mapId, "/Maps/nukieplanet.yml");

        if (loadedOutpost == null)
        {
            Logger.ErrorS("nukies", $"Error loading map {path} for nukies!");
            return;
        }
        _outpost = loadedOutpost;

        // Listen I just don't want it to overlap.
        var (_, shuttleId) = _mapLoader.LoadBlueprint(mapId, shuttlePath, new MapLoadOptions()
        {
            Offset = Vector2.One * 1000f,
        });

        if (TryComp<ShuttleComponent>(shuttleId, out var shuttle))
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>().TryFTLDock(shuttle, _outpost.Value);
        }
    }

    //For admins forcing someone to nukeOps.
    public void MakeLoneNukie(Mind.Mind mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return;

        mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(NukeopsPrototypeId)));
        _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull"), null);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.NukeopsMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    public override void Started()
    {
        _opsWon = false;
    }

    public override void Ended() { }
}

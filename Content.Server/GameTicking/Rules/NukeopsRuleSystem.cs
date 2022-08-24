using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Traitor;
using System.Data;
using Content.Server.Traitor.Uplink;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private Dictionary<Mind.Mind, bool> _aliveNukeops = new();
    private bool _opsWon;

    public override string Prototype => "Nukeops";

    private readonly SoundSpecifier _greetSound = new SoundPathSpecifier("/Audio/Misc/nukeops.ogg");

    private const string NukeopsPrototypeId = "Nukeops";
    private const string NukeopsCommanderPrototypeId = "NukeopsCommander";

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

        // Basically copied verbatim from traitor code
        var playersPerOperative = _cfg.GetCVar(CCVars.NukeopsPlayersPerOp);
        var maxOperatives = _cfg.GetCVar(CCVars.NukeopsMaxOps);

        var everyone = new List<IPlayerSession>(ev.PlayerPool);
        var prefList = new List<IPlayerSession>();
        var cmdrPrefList = new List<IPlayerSession>();
        var operatives = new List<IPlayerSession>();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in everyone)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(NukeopsPrototypeId))
            {
                prefList.Add(player);
            }
            if (profile.AntagPreferences.Contains(NukeopsCommanderPrototypeId))
            {
                cmdrPrefList.Add(player);
            }
        }

        var numNukies = MathHelper.Clamp(ev.PlayerPool.Count / playersPerOperative, 1, maxOperatives);

        for (var i = 0; i < numNukies; i++)
        {
            IPlayerSession nukeOp;
            // Only one commander, so we do it at the start
            if (i == 0)
            {
                if (cmdrPrefList.Count == 0)
                {
                    if (prefList.Count == 0)
                    {
                        if (everyone.Count == 0)
                        {
                            Logger.InfoS("preset", "Insufficient ready players to fill up with nukeops, stopping the selection");
                            break;
                        }
                        nukeOp = _random.PickAndTake(everyone);
                        Logger.InfoS("preset", "Insufficient preferred nukeop commanders or nukies, picking at random.");
                    }
                    else
                    {
                        nukeOp = _random.PickAndTake(prefList);
                        everyone.Remove(nukeOp);
                        Logger.InfoS("preset", "Insufficient preferred nukeop commanders, picking at random from regular op list.");
                    }
                }
                else
                {
                    nukeOp = _random.PickAndTake(cmdrPrefList);
                    everyone.Remove(nukeOp);
                    prefList.Remove(nukeOp);
                    Logger.InfoS("preset", "Selected a preferred nukeop commander.");
                }
            }
            else
            {
                if (prefList.Count == 0)
                {
                    if (everyone.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient ready players to fill up with nukeops, stopping the selection");
                        break;
                    }
                    nukeOp = _random.PickAndTake(everyone);
                    Logger.InfoS("preset", "Insufficient preferred nukeops, picking at random.");
                }
                else
                {
                    nukeOp = _random.PickAndTake(prefList);
                    everyone.Remove(nukeOp);
                    Logger.InfoS("preset", "Selected a preferred nukeop.");
                }
            }
            operatives.Add(nukeOp);
        }

        // TODO: Make this a prototype
        // so true PAUL!
        var path = "/Maps/nukieplanet.yml";
        var shuttlePath = "/Maps/infiltrator.yml";
        var mapId = _mapManager.CreateMap();

        var (_, outpost) = _mapLoader.LoadBlueprint(mapId, "/Maps/nukieplanet.yml");

        if (outpost == null)
        {
            Logger.ErrorS("nukies", $"Error loading map {path} for nukies!");
            return;
        }

        // Listen I just don't want it to overlap.
        var (_, shuttleId) = _mapLoader.LoadBlueprint(mapId, shuttlePath, new MapLoadOptions()
        {
            Offset = Vector2.One * 1000f,
        });

        // Naughty, someone saved the shuttle as a map.
        if (Deleted(outpost))
        {
            Logger.ErrorS("nukeops", $"Tried to load nukeops shuttle as a map, aborting.");
            _mapManager.DeleteMap(mapId);
            return;
        }

        if (TryComp<ShuttleComponent>(shuttleId, out var shuttle))
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>().TryFTLDock(shuttle, outpost.Value);
        }

        // TODO: Loot table or something
        var commanderGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateCommanderGearFull");
        var starterGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull");
        var medicGear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeMedicFull");
        var syndicateNamesElite = new List<string>(_prototypeManager.Index<DatasetPrototype>("SyndicateNamesElite").Values);
        var syndicateNamesNormal = new List<string>(_prototypeManager.Index<DatasetPrototype>("SyndicateNamesNormal").Values);

        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != "SpawnPointNukies") continue;

            if (xform.ParentUid == outpost)
            {
                spawns.Add(xform.Coordinates);
                break;
            }
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(outpost.Value).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }

        // TODO: This should spawn the nukies in regardless and transfer if possible; rest should go to shot roles.
        for (var i = 0; i < operatives.Count; i++)
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

            var session = operatives[i];
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);
            newMind.AddRole(new TraitorRole(newMind, _prototypeManager.Index<AntagPrototype>(role)));

            var mob = EntityManager.SpawnEntity("MobHuman", _random.Pick(spawns));
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;
            EntityManager.AddComponent<RandomHumanoidAppearanceComponent>(mob);

            newMind.TransferTo(mob);
            _stationSpawningSystem.EquipStartingGear(mob, gear, null);

            ev.PlayerPool.Remove(session);
            _aliveNukeops.Add(newMind, true);

            GameTicker.PlayerJoinGame(session);
        }

        SoundSystem.Play(_greetSound.GetSound(), Filter.Empty().AddWhere(s =>
        {
            var mind = ((IPlayerSession) s).Data.ContentData()?.Mind;
            return mind != null && _aliveNukeops.ContainsKey(mind);
        }), AudioParams.Default);
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

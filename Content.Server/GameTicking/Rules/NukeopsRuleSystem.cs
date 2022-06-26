using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
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
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    private Dictionary<Mind.Mind, bool> _aliveNukeops = new();
    private bool _opsWon;

    public override string Prototype => "Nukeops";

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
    	if (!Enabled)
            return;

        _opsWon = true;
        _roundEndSystem.EndRound();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!Enabled)
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
        if (!Enabled)
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
        if (!Enabled)
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
            if(player.ContentData()?.Mind?.AllRoles.All(role => role is not Job {CanBeAntag: false}) ?? false) continue;
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
        var map = "/Maps/infiltrator.yml";

        var center = new Vector2();
        var minRadius = 0f;
        Box2? aabb = null;

        foreach (var uid in _stationSystem.Stations)
        {
            if (TryComp<StationDataComponent>(uid, out var stationData))
            {
                foreach (var grid in stationData.Grids)
                {
                    if (TryComp<IMapGridComponent>(grid, out var gridComp))
                        aabb = aabb?.Union(gridComp.Grid.WorldAABB) ?? gridComp.Grid.WorldAABB;
                }
            }
        }

        if (aabb != null)
        {
            center = aabb.Value.Center;
            minRadius = MathF.Max(aabb.Value.Width, aabb.Value.Height);
        }

        var (_, gridId) = _mapLoader.LoadBlueprint(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = center + MathF.Max(minRadius, minRadius) + 1000f,
        });

        if (!gridId.HasValue)
        {
            Logger.ErrorS("NUKEOPS", $"Gridid was null when loading \"{map}\", aborting.");
            foreach (var session in operatives)
            {
                ev.PlayerPool.Add(session);
            }
            return;
        }

        var gridUid = _mapManager.GetGridEuid(gridId.Value);
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
            if (meta.EntityPrototype?.ID != "SpawnPointNukies" || xform.ParentUid != gridUid) continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(gridUid).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }

        // TODO: This should spawn the nukies in regardless and transfer if possible; rest should go to shot roles.
        for (var i = 0; i < operatives.Count; i++)
        {
            string name;
            StartingGearPrototype gear;

            switch (i)
            {
                case 0:
                    name = $"Commander " + _random.PickAndTake<string>(syndicateNamesElite);
                    gear = commanderGear;
                    break;
                case 1:
                    name = $"Agent " + _random.PickAndTake<string>(syndicateNamesNormal);
                    gear = medicGear;
                    break;
                default:
                    name = $"Operator " + _random.PickAndTake<string>(syndicateNamesNormal);
                    gear = starterGear;
                    break;
            }

            var session = operatives[i];
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);

            var mob = EntityManager.SpawnEntity("MobHuman", _random.Pick(spawns));
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;
            EntityManager.AddComponent<RandomHumanoidAppearanceComponent>(mob);

            newMind.TransferTo(mob);
            _stationSpawningSystem.EquipStartingGear(mob, gear, null);

            ev.PlayerPool.Remove(session);
            _aliveNukeops.Add(newMind, true);

            GameTicker.PlayerJoinGame(session);
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!Enabled)
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


    public override void Started(GameRuleConfiguration _)
    {
        _opsWon = false;
    }

    public override void Ended(GameRuleConfiguration _) { }
}

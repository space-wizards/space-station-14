using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.MobState;
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
    	if (!Enabled) return;
    
        _opsWon = true;
        _roundEndSystem.EndRound();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        ev.AddLine(_opsWon ? Loc.GetString("nukeops-ops-won") : Loc.GetString("nukeops-crew-won"));
        ev.AddLine(Loc.GetString("nukeops-list-start"));
        foreach (var nukeop in _aliveNukeops)
        {
            ev.AddLine($"- {nukeop.Key.Session?.Name}");
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!_aliveNukeops.TryFirstOrNull(x => x.Key.OwnedEntity == ev.Entity, out var op)) return;

        _aliveNukeops[op.Value.Key] = op.Value.Key.CharacterDeadIC;

        if (_aliveNukeops.Values.All(x => !x))
        {
            _roundEndSystem.EndRound();
        }
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        _aliveNukeops.Clear();

        var numOps = (int)Math.Min(Math.Floor((double)ev.PlayerPool.Count / _cfg.GetCVar(CCVars.NukeopsPlayersPerOp)),
            _cfg.GetCVar(CCVars.NukeopsMaxOps));
        var ops = new IPlayerSession[numOps];
        for (var i = 0; i < numOps; i++)
        {
            ops[i] = _random.PickAndTake(ev.PlayerPool);
        }

        var map = "/Maps/syndiepuddle.yml";

        var aabbs = _stationSystem.Stations.SelectMany(x =>
            Comp<StationDataComponent>(x).Grids.Select(x => _mapManager.GetGridComp(x).Grid.WorldBounds.CalcBoundingBox())).ToArray();
        var aabb = aabbs[0];
        for (int i = 1; i < aabbs.Length; i++)
        {
            aabb.Union(aabbs[i]);
        }

        var (_, grid) = _mapLoader.LoadBlueprint(GameTicker.DefaultMap, map, new MapLoadOptions
        {
            Offset = aabb.Center + MathF.Max(aabb.Height, aabb.Width)*2.5f
        });

        if (!grid.HasValue)
        {
            Logger.ErrorS("NUKEOPS", $"Gridid was null when loading \"{map}\", aborting.");
            foreach (var session in ops)
            {
                ev.PlayerPool.Add(session);
            }
            return;
        }

        var gear = _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull");
        var spawnpos = new EntityCoordinates(_mapManager.GetGridEuid(grid.Value), new Vector2(1.5f, -4.5f));
        for (var i = 0; i < ops.Length; i++)
        {
            var session = ops[i];
            var name = $"Operator #{i}";
            var newMind = new Mind.Mind(session.UserId)
            {
                CharacterName = name
            };
            newMind.ChangeOwningPlayer(session.UserId);

            var mob = EntityManager.SpawnEntity("MobHuman", spawnpos);
            EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;

            newMind.TransferTo(mob);
            _stationSpawningSystem.EquipStartingGear(mob, gear, null);

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


    public override void Started(){ _opsWon = false; }

    public override void Ended(){ }
}

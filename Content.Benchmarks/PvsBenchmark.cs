#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Mind;
using Content.Server.Warps;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Benchmarks;

// This benchmark probably benefits from some accidental cache locality. I,e. the order in which entities in a pvs
// chunk are sent to players matches the order in which the entities were spawned.
//
// in a real mid-late game round, this is probably no longer the case.
// One way to somewhat offset this is to update the NetEntity assignment to assign random (but still unique) NetEntity uids to entities.
// This makes the benchmark run noticeably slower.

[Virtual]
public class PvsBenchmark
{
    public const string Map = "Maps/box.yml";

    [Params(1, 8, 80)]
    public int PlayerCount { get; set; }

    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private MapId _mapId = new(10);
    private ICommonSession[] _players = default!;
    private EntityCoordinates[] _spawns = default!;
    public int _cycleOffset = 0;
    private SharedTransformSystem _sys = default!;
    private EntityCoordinates[] _locations = default!;

    [GlobalSetup]
    public void Setup()
    {
#if !DEBUG
        ProgramShared.PathOffset = "../../../../";
#endif
        PoolManager.Startup();

        _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
        _entMan = _pair.Server.ResolveDependency<IEntityManager>();
        _pair.Server.CfgMan.SetCVar(CVars.NetPVS, true);
        _pair.Server.CfgMan.SetCVar(CVars.ThreadParallelCount, 0);
        _pair.Server.CfgMan.SetCVar(CVars.NetPvsAsync, false);
        _sys = _entMan.System<SharedTransformSystem>();

        SetupAsync().Wait();
    }

    private async Task SetupAsync()
    {
        // Spawn the map
        _pair.Server.ResolveDependency<IRobustRandom>().SetSeed(42);
        await _pair.Server.WaitPost(() =>
        {
            var success = _entMan.System<MapLoaderSystem>().TryLoad(_mapId, Map, out _);
            if (!success)
                throw new Exception("Map load failed");
            _pair.Server.MapMan.DoMapInitialize(_mapId);
        });

        // Get list of ghost warp positions
        _spawns = _entMan.AllComponentsList<WarpPointComponent>()
            .OrderBy(x => x.Component.Location)
            .Select(x => _entMan.GetComponent<TransformComponent>(x.Uid).Coordinates)
            .ToArray();

        Array.Resize(ref _players, PlayerCount);

        // Spawn "Players"
        _players = await _pair.Server.AddDummySessions(PlayerCount);
        await _pair.Server.WaitPost(() =>
        {
            var mind = _pair.Server.System<MindSystem>();
            for (var i = 0; i < PlayerCount; i++)
            {
                var pos = _spawns[i % _spawns.Length];
                var uid =_entMan.SpawnEntity("MobHuman", pos);
                _pair.Server.ConsoleHost.ExecuteCommand($"setoutfit {_entMan.GetNetEntity(uid)} CaptainGear");
                mind.ControlMob(_players[i].UserId, uid);
            }
        });

        // Repeatedly move players around so that they "explore" the map and see lots of entities.
        // This will populate their PVS data with out-of-view entities.
        var rng = new Random(42);
        ShufflePlayers(rng, 100);

        _pair.Server.PvsTick(_players);
        _pair.Server.PvsTick(_players);

        var ents = _players.Select(x => x.AttachedEntity!.Value).ToArray();
        _locations = ents.Select(x => _entMan.GetComponent<TransformComponent>(x).Coordinates).ToArray();
    }

    private void ShufflePlayers(Random rng, int count)
    {
        while (count > 0)
        {
            ShufflePlayers(rng);
            count--;
        }
    }

    private void ShufflePlayers(Random rng)
    {
        _pair.Server.PvsTick(_players);

        var ents = _players.Select(x => x.AttachedEntity!.Value).ToArray();
        var locations = ents.Select(x => _entMan.GetComponent<TransformComponent>(x).Coordinates).ToArray();

        // Shuffle locations
        var n = locations.Length;
        while (n > 1)
        {
            n -= 1;
            var k = rng.Next(n + 1);
            (locations[k], locations[n]) = (locations[n], locations[k]);
        }

        _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < PlayerCount; i++)
            {
                _sys.SetCoordinates(ents[i], locations[i]);
            }
        }).Wait();

        _pair.Server.PvsTick(_players);
    }

    /// <summary>
    /// Basic benchmark for PVS in a static situation where nothing moves or gets dirtied..
    /// This effectively provides a lower bound on "real" pvs tick time, as it is missing:
    /// - PVS chunks getting dirtied and needing to be rebuilt
    /// - Fetching component states for dirty components
    /// - Compressing & sending network messages
    /// - Sending PVS leave messages
    /// </summary>
    [Benchmark]
    public void StaticTick()
    {
        _pair.Server.PvsTick(_players);
    }

    /// <summary>
    /// Basic benchmark for PVS in a situation where players are teleporting all over the place. This isn't very
    /// realistic, but unlike <see cref="StaticTick"/> this will actually also measure the speed of processing dirty
    /// chunks and sending PVS leave messages.
    /// </summary>
    [Benchmark]
    public void CycleTick()
    {
        _cycleOffset = (_cycleOffset + 1) % _players.Length;
        _pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < PlayerCount; i++)
            {
                _sys.SetCoordinates(_players[i].AttachedEntity!.Value, _locations[(i + _cycleOffset) % _players.Length]);
            }
        }).Wait();
        _pair.Server.PvsTick(_players);
    }
}

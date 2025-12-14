using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Destructible;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Benchmarks;

[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class DestructibleBenchmark
{
    /// <summary>
    /// Number of destructible entities per prototype to spawn with a <see cref="DestructibleComponent"/>.
    /// </summary>
    [Params(1, 10, 100, 1000, 5000)]
    public int EntityCount;

    /// <summary>
    /// Amount of blunt damage we do to each entity.
    /// </summary>
    [Params(10000)]
    public FixedPoint2 DamageAmount;

    [Params("Blunt")]
    public ProtoId<DamageTypePrototype> DamageType;

    private static readonly EntProtoId WindowProtoId = "Window";
    private static readonly EntProtoId WallProtoId = "WallReinforced";
    private static readonly EntProtoId HumanProtoId = "MobHuman";

    private static readonly ProtoId<ContentTileDefinition> TileRef = "Plating";

    private readonly EntProtoId[] _prototypes = [WindowProtoId, WallProtoId, HumanProtoId];

    private readonly List<Entity<DamageableComponent>> _damageables = new();
    private readonly List<Entity<DamageableComponent, DestructibleComponent>> _destructbiles = new();

    private DamageSpecifier _damage;

    private TestPair _pair = default!;
    private IEntityManager _entMan = default!;
    private IPrototypeManager _protoMan = default!;
    private IRobustRandom _random = default!;
    private ITileDefinitionManager _tileDefMan = default!;
    private DamageableSystem _damageable = default!;
    private DestructibleSystem _destructible = default!;
    private SharedMapSystem _map = default!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var mapdata = await _pair.CreateTestMap();

        _entMan = server.ResolveDependency<IEntityManager>();
        _protoMan = server.ResolveDependency<IPrototypeManager>();
        _random = server.ResolveDependency<IRobustRandom>();
        _tileDefMan = server.ResolveDependency<ITileDefinitionManager>();
        _damageable = _entMan.System<DamageableSystem>();
        _destructible = _entMan.System<DestructibleSystem>();
        _map = _entMan.System<SharedMapSystem>();

        if (!_protoMan.Resolve(DamageType, out var type))
            return;

        _damage = new DamageSpecifier(type, DamageAmount);

        _random.SetSeed(69420); // Randomness needs to be deterministic for benchmarking.

        var plating = _tileDefMan[TileRef].TileId;

        // We make a rectangular grid of destructible entities, and then damage them all simultaneously to stress test the system.
        // Needed for managing the performance of destructive effects and damage application.
        await server.WaitPost(() =>
        {
            // Set up a thin line of tiles to place our objects on. They should be anchored for a "realistic" scenario...
            for (var x = 0; x < EntityCount; x++)
            {
                for (var y = 0; y < _prototypes.Length; y++)
                {
                    _map.SetTile(mapdata.Grid, mapdata.Grid, new Vector2i(x, y), new Tile(plating));
                }
            }

            for (var x = 0; x < EntityCount; x++)
            {
                var y = 0;
                foreach (var protoId in _prototypes)
                {
                    var coords = new EntityCoordinates(mapdata.Grid, x + 0.5f, y + 0.5f);
                    _entMan.SpawnEntity(protoId, coords);
                    y++;
                }
            }

            var query = _entMan.EntityQueryEnumerator<DamageableComponent, DestructibleComponent>();

            while (query.MoveNext(out var uid, out var damageable, out var destructible))
            {
                _damageables.Add((uid, damageable));
                _destructbiles.Add((uid, damageable, destructible));
            }
        });
    }

    [Benchmark]
    public async Task PerformDealDamage()
    {
        await _pair.Server.WaitPost(() =>
        {
            _damageable.ApplyDamageToAllEntities(_damageables, _damage);
        });
    }

    [Benchmark]
    public async Task PerformTestTriggers()
    {
        await _pair.Server.WaitPost(() =>
        {
            _destructible.TestAllTriggers(_destructbiles);
        });
    }

    [Benchmark]
    public async Task PerformTestBehaviors()
    {
        await _pair.Server.WaitPost(() =>
        {
            _destructible.TestAllBehaviors(_destructbiles);
        });
    }


    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.Warps;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Benchmarks;

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
    private EntityCoordinates[] _warps = default!;

    [GlobalSetup]
    public void Setup()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_pair == null)
        {
#if !DEBUG
        ProgramShared.PathOffset = "../../../../";
#endif
            PoolManager.Startup(null);

            _pair = PoolManager.GetServerClient().GetAwaiter().GetResult();
            _entMan = _pair.Server.ResolveDependency<IEntityManager>();
            _pair.Server.CfgMan.SetCVar(CVars.NetPVS, true);
            _pair.Server.CfgMan.SetCVar(CVars.ThreadParallelCount, 0);

            // Spawn the map
            _pair.Server.ResolveDependency<IRobustRandom>().SetSeed(42);
            _pair.Server.WaitPost(() =>
            {
                var success = _entMan.System<MapLoaderSystem>().TryLoad(_mapId, Map, out _);
                if (!success)
                    throw new Exception("Map load failed");
                _pair.Server.MapMan.DoMapInitialize(_mapId);
            }).Wait();

            // Get list of ghost warp positions
            _warps = _entMan.AllComponentsList<WarpPointComponent>()
                .OrderBy(x => x.Component.Location)
                .Select(x => _entMan.GetComponent<TransformComponent>(x.Uid).Coordinates)
                .ToArray();
        }

        if (PlayerCount < (_players?.Length ?? 0))
            throw new Exception($"Player counts have to be increasing");

        var start = _players == null ? 0 : (_players.Length - 1);
        Array.Resize(ref _players, PlayerCount);

        // Spawn "Players".
        _pair.Server.WaitPost(() =>
        {
            for (var i = start; i < PlayerCount; i++)
            {
                var pos = _warps[i % _warps.Length];
                var uid =_entMan.SpawnEntity("MobHuman", pos);
                var nuid = _entMan.GetNetEntity(uid);
                _pair.Server.ConsoleHost.ExecuteCommand($"setoutfit {nuid} CaptainGear");
                _players![i] = new DummySession{AttachedEntity = uid};
            }
        }).Wait();

        PvsTick();
        PvsTick();
        PvsTick();
    }

    /// <summary>
    /// Basic benchmark for PVS in a static situation where nothing moves or gets dirtied..
    /// This effectively provides a lower bound on "real" pvs tick time, as itt is missing:
    /// - PVS chunks getting dirtied and needing to be rebuilt
    /// - Fetching component states for dirty components
    /// - Compressing & sending network messages
    /// - Sending PVS leave messages
    /// </summary>
    [Benchmark]
    public void PvsTick()
    {
        _pair.Server.PvsTick(_players);
    }

    private sealed class DummySession : ICommonSession
    {
        public SessionStatus Status => SessionStatus.InGame;
        public EntityUid? AttachedEntity {get; set; }
        public NetUserId UserId => default;
        public string Name { get; } = string.Empty;
        public short Ping => default;
        public INetChannel Channel { get; set; } = default!;
        public LoginType AuthType => default;
        public HashSet<EntityUid> ViewSubscriptions { get; } = new();
        public DateTime ConnectedTime { get; set; }
        public SessionState State => default!;
        public SessionData Data => default!;
        public bool ClientSide { get; set; }
    }
}

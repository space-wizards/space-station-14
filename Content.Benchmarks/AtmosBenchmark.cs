using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Engines;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Benchmarks
{
    public class AtmosBenchmark : ContentBenchmark
    {
        private const string TileName = "floor_steel";
        private const string WallPrototype = "reinforced_wall";
        private const int SquareSize = 32;
        private const int Ticks = 30 * 60; // 1 minute in 30 TPS

        private readonly Dictionary<Gas, float> _gases = new Dictionary<Gas, float>()
        {
            { Gas.Oxygen, 10000f },
            { Gas.Phoron, 10000f }
        };

        private float _temperature = 10000f;
        private ServerIntegrationInstance _server;

        private int HalfSquareSize => SquareSize / 2;

        [GlobalSetup(Target = nameof(PhoronFireBenchmarkNaive))]
        public void SetupNaive()
        {
            SetEnv(false);
            Setup();
        }

        [GlobalSetup(Target = nameof(PhoronFireBenchmarkSse))]
        public void SetupSse()
        {
            if (!Sse.IsSupported)
            {
                throw new NotSupportedException("SSE is not supported!");
            }

            SetEnv(true);
            Setup();
        }

        [GlobalSetup(Target = nameof(PhoronFireBenchmarkAvx))]
        public void SetupAvx()
        {
            if (!Avx.IsSupported)
            {
                throw new NotSupportedException("AVX is not supported!");
            }

            SetEnv(true, true);
            Setup();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _server = StartServerDummyTicker();
        }

        [Benchmark(Baseline = true)]
        public void PhoronFireBenchmarkNaive()
        {
            _server.Loop(AtmosFire, Ticks);
        }

        [Benchmark]
        public void PhoronFireBenchmarkSse()
        {
            _server.Loop(AtmosFire, Ticks);
        }

        [Benchmark]
        public void PhoronFireBenchmarkAvx()
        {
            _server.Loop(AtmosFire, Ticks);
        }

        /// <summary>
        ///     Forces the NumericsHelpers static constructor to run again after setting AVX to enabled or disabled.
        /// </summary>
        private void SetEnv(bool enabled, bool avxEnabled = false)
        {
            Environment.SetEnvironmentVariable(NumericsHelpers.DisabledEnvironmentVariable, enabled ? null : "true");
            Environment.SetEnvironmentVariable(NumericsHelpers.AvxEnvironmentVariable, avxEnabled ? "true" : null);
            var numerics = typeof(NumericsHelpers);
            numerics.TypeInitializer?.Invoke(null, null);
        }

        private void AtmosFire()
        {
            bool CoordinateWall(int x) => x == -HalfSquareSize || x == (HalfSquareSize - 1);
            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var pauseManager = IoCManager.Resolve<IPauseManager>();

            foreach (var mapId in mapManager.GetAllMapIds())
            {
                   pauseManager.SetMapPaused(mapId, true);
            }

            var newMap = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(newMap);
            var map = mapManager.GetMapEntity(newMap);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);

            var tile = new Tile(tileDefinitionManager[TileName].TileId);

            var gridAtmos = gridEnt.AddComponent<GridAtmosphereComponent>();

            for (var x = -HalfSquareSize; x < HalfSquareSize; x++)
            {
                for (var y = -HalfSquareSize; y < HalfSquareSize; y++)
                {
                    var vector = new Vector2i(x, y);
                    grid.SetTile(vector, tile);

                    if (CoordinateWall(x) || CoordinateWall(y))
                    {
                        entityManager.SpawnEntity(WallPrototype, new EntityCoordinates(gridEnt.Uid, vector));
                    }
                }
            }

            gridAtmos.RepopulateTiles();
            gridAtmos.Update(0f);

            var zeroAtmos = gridAtmos.GetTile(Vector2i.Zero);

            if (zeroAtmos == null)
                throw new NullReferenceException("Atmosphere at (0, 0) is null!");

            foreach (var (gas, amount) in _gases)
            {
                zeroAtmos.Air.AdjustMoles(gas, amount);
            }

            zeroAtmos.Air.Temperature = _temperature;

            gridAtmos.AddActiveTile(zeroAtmos);

            gridAtmos.Invalidate(Vector2i.Zero);
        }
    }
}

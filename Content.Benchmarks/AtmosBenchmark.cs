using System;
using System.Collections.Generic;
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
    [SimpleJob(RunStrategy.Monitoring)]
    public class AtmosBenchmark : ContentBenchmark
    {
        private string _tileName = "floor_steel";
        private string _wallPrototype = "reinforced_wall";
        private int _squareSize = 32;
        private int _ticks = 30 * 60; // 1 minute in 30 TPS

        private Dictionary<Gas, float> _gases = new Dictionary<Gas, float>()
        {
            { Gas.Oxygen, 10000f },
            { Gas.Phoron, 10000f }
        };

        private float _temperature = 10000f;
        private ServerIntegrationInstance _server;

        private int HalfSquareSize => _squareSize / 2;

        [IterationSetup]
        public void IterationSetup()
        {
            _server = StartServer();
        }

        [Benchmark(Baseline = true)]
        public void PhoronFireBenchmarkNaive()
        {
            NumericsHelpers.Enabled = false;
            NumericsHelpers.AvxEnabled = false;
            _server.Loop(AtmosFire, _ticks);
        }

        [Benchmark]
        public void PhoronFireBenchmarkSse()
        {
            if (!Sse.IsSupported)
            {
                throw new NotSupportedException("SSE is not supported!");
            }

            NumericsHelpers.Enabled = true;
            NumericsHelpers.AvxEnabled = false;
            _server.Loop(AtmosFire, _ticks);
        }

        [Benchmark]
        public void PhoronFireBenchmarkAvx()
        {
            if (!Avx.IsSupported)
            {
                throw new NotSupportedException("AVX is not supported!");
            }

            NumericsHelpers.Enabled = true;
            NumericsHelpers.AvxEnabled = true;
            _server.Loop(AtmosFire, _ticks);
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

            var tile = new Tile(tileDefinitionManager[_tileName].TileId);

            var gridAtmos = gridEnt.AddComponent<GridAtmosphereComponent>();

            for (var x = -HalfSquareSize; x < HalfSquareSize; x++)
            {
                for (var y = -HalfSquareSize; y < HalfSquareSize; y++)
                {
                    var vector = new Vector2i(x, y);
                    grid.SetTile(vector, tile);

                    if (CoordinateWall(x) || CoordinateWall(y))
                    {
                        entityManager.SpawnEntity(_wallPrototype, new EntityCoordinates(gridEnt.Uid, vector));
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

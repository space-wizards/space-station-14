using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Threading.Tasks;
using Content.Client.Markers;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.MapRenderer.Painters
{
    public sealed class MapPainter : IAsyncDisposable
    {
        private readonly RenderMap _map;
        private readonly ITestContextLike _testContextLike;

        private TestPair? _pair;
        private Entity<MapGridComponent>[] _grids = [];

        public MapPainter(RenderMap map, ITestContextLike testContextLike)
        {
            _map = map;
            _testContextLike = testContextLike;
        }

        public async Task Initialize()
        {
            var stopwatch = RStopwatch.StartNew();

            var poolSettings = new PoolSettings
            {
                DummyTicker = false,
                Connected = true,
                Destructive = true,
                Fresh = true,
                // Seriously whoever made MapPainter use GameMapPrototype I wish you step on a lego one time.
                Map = _map is RenderMapPrototype prototype ? prototype.Prototype : PoolManager.TestMap,
            };
            _pair = await PoolManager.GetServerClient(poolSettings, _testContextLike);

            Console.WriteLine($"Loaded client and server in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");

            if (_map is RenderMapFile mapFile)
            {
                using var stream = File.OpenRead(mapFile.FileName);

                await _pair.Server.WaitPost(() =>
                {
                    var loadOptions = new MapLoadOptions
                    {
                        // Accept loading both maps and grids without caring about what the input file truly is.
                        DeserializationOptions =
                        {
                            LogOrphanedGrids = false,
                        },
                    };

                    if (!_pair.Server.System<MapLoaderSystem>().TryLoadGeneric(stream, mapFile.FileName, out var loadResult, loadOptions))
                        throw new IOException($"File {mapFile.FileName} could not be read");

                    _grids = loadResult.Grids.ToArray();
                });
            }
        }

        public async Task SetupView(bool showMarkers)
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            await _pair.Client.WaitPost(() =>
            {
                if (_pair.Client.EntMan.TryGetComponent(_pair.Client.PlayerMan.LocalEntity, out SpriteComponent? sprite))
                {
                    _pair.Client.System<SpriteSystem>()
                        .SetVisible((_pair.Client.PlayerMan.LocalEntity.Value, sprite), false);
                }
            });

            if (showMarkers)
            {
                await _pair.Client.WaitPost(() =>
                {
                    _pair.Client.System<MarkerSystem>().MarkersVisible = true;
                });
            }
        }

        public async Task<MapViewerData> GenerateMapViewerData(ParallaxOutput? parallaxOutput)
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            var mapShort = _map.ShortName;

            string fullName;
            if (_map is RenderMapPrototype prototype)
            {
                fullName = _pair.Server.ProtoMan.Index(prototype.Prototype).MapName;
            }
            else
            {
                fullName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mapShort);
            }

            var mapViewerData = new MapViewerData
            {
                Id = mapShort,
                Name = fullName,
            };

            if (parallaxOutput != null)
            {
                await _pair.Client.WaitPost(() =>
                {
                    var res = _pair.Client.InstanceDependencyCollection.Resolve<IResourceManager>();
                    mapViewerData.ParallaxLayers.Add(LayerGroup.DefaultParallax(res, parallaxOutput));
                });
            }

            return mapViewerData;
        }

        public async IAsyncEnumerable<RenderedGridImage<Rgba32>> Paint()
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            var client = _pair.Client;
            var server = _pair.Server;

            var sEntityManager = server.ResolveDependency<IServerEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapSys = entityManager.System<SharedMapSystem>();

            await _pair.RunTicksSync(10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sMapManager = server.ResolveDependency<IMapManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new GridPainter(client, server);
            var xformQuery = sEntityManager.GetEntityQuery<TransformComponent>();
            var xformSystem = sEntityManager.System<SharedTransformSystem>();

            await server.WaitPost(() =>
            {
                var playerEntity = sPlayerManager.Sessions.Single().AttachedEntity;

                if (playerEntity.HasValue)
                {
                    sEntityManager.DeleteEntity(playerEntity.Value);
                }

                if (_map is RenderMapPrototype)
                {
                    var mapId = sEntityManager.System<GameTicker>().DefaultMap;
                    _grids = sMapManager.GetAllGrids(mapId).ToArray();
                }

                foreach (var (uid, _) in _grids)
                {
                    var gridXform = xformQuery.GetComponent(uid);
                    xformSystem.SetWorldRotation(gridXform, Angle.Zero);
                }
            });

            await _pair.RunTicksSync(10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            foreach (var (uid, grid) in _grids)
            {
                var tiles = mapSys.GetAllTiles(uid, grid).ToList();
                if (tiles.Count == 0)
                {
                    Console.WriteLine($"Warning: Grid {uid} was empty. Skipping image rendering.");
                    continue;
                }
                var tileXSize = grid.TileSize * TilePainter.TileImageSize;
                var tileYSize = grid.TileSize * TilePainter.TileImageSize;

                var minX = tiles.Min(t => t.X);
                var minY = tiles.Min(t => t.Y);
                var maxX = tiles.Max(t => t.X);
                var maxY = tiles.Max(t => t.Y);
                var w = (maxX - minX + 1) * tileXSize;
                var h = (maxY - minY + 1) * tileYSize;
                var customOffset = new Vector2();

                //MapGrids don't have LocalAABB, so we offset them to align the bottom left corner with 0,0 coordinates
                if (grid.LocalAABB.IsEmpty())
                    customOffset = new Vector2(-minX, -minY);

                var gridCanvas = new Image<Rgba32>(w, h);

                await server.WaitPost(() =>
                {
                    tilePainter.Run(gridCanvas, uid, grid, customOffset);
                    entityPainter.Run(gridCanvas, uid, grid, customOffset);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));
                });

                var renderedImage = new RenderedGridImage<Rgba32>(gridCanvas)
                {
                    GridUid = uid,
                    Offset = xformSystem.GetWorldPosition(uid),
                };

                yield return renderedImage;
            }
        }

        public async Task CleanReturnAsync()
        {
            if (_pair == null)
                throw new InvalidOperationException("Instance not initialized!");

            await _pair.CleanReturnAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_pair != null)
                await _pair.DisposeAsync();
        }
    }
}

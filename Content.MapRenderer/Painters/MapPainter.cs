using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.Server.GameTicking;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.MapRenderer.Painters
{
    public sealed class MapPainter
    {
        public static async IAsyncEnumerable<RenderedGridImage<Rgba32>> Paint(string map,
            bool mapIsFilename = false,
            bool showMarkers = false)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                DummyTicker = false,
                Connected = true,
                Fresh = true,
                // Seriously whoever made MapPainter use GameMapPrototype I wish you step on a lego one time.
                Map = mapIsFilename ? "Empty" : map,
            });

            var server = pair.Server;
            var client = pair.Client;

            Console.WriteLine($"Loaded client and server in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();

            var cEntityManager = client.ResolveDependency<IClientEntityManager>();
            var cPlayerManager = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();

            await client.WaitPost(() =>
            {
                if (cEntityManager.TryGetComponent(cPlayerManager.LocalEntity, out SpriteComponent? sprite))
                {
                    cEntityManager.System<SpriteSystem>().SetVisible((cPlayerManager.LocalEntity.Value, sprite), false);
                }
            });

            if (showMarkers)
                await pair.WaitClientCommand("showmarkers");

            var sEntityManager = server.ResolveDependency<IServerEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entityManager.System<MapLoaderSystem>();
            var mapSys = entityManager.System<SharedMapSystem>();
            var deps = server.ResolveDependency<IEntitySystemManager>().DependencyCollection;

            Entity<MapGridComponent>[] grids = [];

            if (mapIsFilename)
            {
                var resPath = new ResPath(map);

                if (!mapLoader.TryReadFile(resPath, out var data))
                    throw new IOException($"File {map} could not be read");

                var ev = new BeforeEntityReadEvent();
                server.EntMan.EventBus.RaiseEvent(EventSource.Local, ev);

                var deserializer = new EntityDeserializer(deps,
                    data,
                    DeserializationOptions.Default,
                    ev.RenamedPrototypes,
                    ev.DeletedPrototypes);

                if (!deserializer.TryProcessData())
                {
                    throw new IOException($"Failed to process entity data in {map}");
                }

                if (deserializer.Result.Category == FileCategory.Unknown && deserializer.Result.Version < 7)
                {
                    var mapCount = 0;
                    var gridCount = 0;
                    foreach (var (entId, ent) in deserializer.YamlEntities)
                    {
                        if (ent.Components != null && ent.Components.ContainsKey("MapGrid"))
                        {
                            gridCount++;
                        }
                        if (ent.Components != null && ent.Components.ContainsKey("Map"))
                        {
                            mapCount++;
                        }
                    }
                    if (mapCount == 1)
                        deserializer.Result.Category = FileCategory.Map;
                    else if (mapCount == 0 && gridCount == 1)
                        deserializer.Result.Category = FileCategory.Grid;
                }

                switch (deserializer.Result.Category)
                {
                    case FileCategory.Map:
                        await server.WaitPost(() =>
                        {
                            if (mapLoader.TryLoadMap(resPath, out _, out var loadedGrids))
                            {
                                grids = loadedGrids.ToArray();
                            }
                        });
                        break;

                    case FileCategory.Grid:
                        await server.WaitPost(() =>
                        {
                            if (mapLoader.TryLoadGrid(resPath, out _, out var loadedGrids))
                            {
                                grids = [(Entity<MapGridComponent>)loadedGrids];
                            }
                        });
                        break;

                    default:
                        throw new IOException($"Unknown category {deserializer.Result.Category}");

                }
            }

            await pair.RunTicksSync(10);
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

                if (!mapIsFilename)
                {
                    var mapId = sEntityManager.System<GameTicker>().DefaultMap;
                    grids = sMapManager.GetAllGrids(mapId).ToArray();
                }

                foreach (var (uid, _) in grids)
                {
                    var gridXform = xformQuery.GetComponent(uid);
                    xformSystem.SetWorldRotation(gridXform, Angle.Zero);
                }
            });

            await pair.RunTicksSync(10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            foreach (var (uid, grid) in grids)
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

            // We don't care if it fails as we have already saved the images.
            try
            {
                await pair.CleanReturnAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}

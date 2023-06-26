#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpriteComponent = Robust.Client.GameObjects.SpriteComponent;

namespace Content.MapRenderer.Painters
{
    public sealed class MapPainter
    {
        public async IAsyncEnumerable<RenderedGridImage<Rgba32>> Paint(string map)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
            {
                Fresh = true,
                Map = map
            });

            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            Console.WriteLine($"Loaded client and server in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();

            var cEntityManager = client.ResolveDependency<IClientEntityManager>();
            var cPlayerManager = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();

            await client.WaitPost(() =>
            {
                if (cEntityManager.TryGetComponent(cPlayerManager.LocalPlayer!.ControlledEntity!, out SpriteComponent? sprite))
                {
                    sprite.Visible = false;
                }
            });

            var sEntityManager = server.ResolveDependency<IServerEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sMapManager = server.ResolveDependency<IMapManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new GridPainter(client, server);
            (EntityUid Uid, MapGridComponent Grid)[] grids = null!;
            var xformQuery = sEntityManager.GetEntityQuery<TransformComponent>();
            var xformSystem = sEntityManager.System<SharedTransformSystem>();

            await server.WaitPost(() =>
            {
                var playerEntity = sPlayerManager.ServerSessions.Single().AttachedEntity;

                if (playerEntity.HasValue)
                {
                    sEntityManager.DeleteEntity(playerEntity.Value);
                }

                var mapId = sMapManager.GetAllMapIds().Last();
                grids = sMapManager.GetAllMapGrids(mapId).Select(o => (o.Owner, o)).ToArray();

                foreach (var grid in grids)
                {
                    var gridXform = xformQuery.GetComponent(grid.Uid);
                    xformSystem.SetWorldRotation(gridXform, Angle.Zero);
                }
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            foreach (var grid in grids)
            {
                // Skip empty grids
                if (grid.Grid.LocalAABB.IsEmpty())
                {
                    Console.WriteLine($"Warning: Grid {grid.Uid} was empty. Skipping image rendering.");
                    continue;
                }

                var tileXSize = grid.Grid.TileSize * TilePainter.TileImageSize;
                var tileYSize = grid.Grid.TileSize * TilePainter.TileImageSize;

                var bounds = grid.Grid.LocalAABB;

                var left = bounds.Left;
                var right = bounds.Right;
                var top = bounds.Top;
                var bottom = bounds.Bottom;

                var w = (int) Math.Ceiling(right - left) * tileXSize;
                var h = (int) Math.Ceiling(top - bottom) * tileYSize;

                var gridCanvas = new Image<Rgba32>(w, h);

                await server.WaitPost(() =>
                {
                    tilePainter.Run(gridCanvas, grid.Uid, grid.Grid);
                    entityPainter.Run(gridCanvas, grid.Uid, grid.Grid);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));
                });

                var renderedImage = new RenderedGridImage<Rgba32>(gridCanvas)
                {
                    GridUid = grid.Uid,
                    Offset = xformSystem.GetWorldPosition(grid.Uid),
                };

                yield return renderedImage;
            }

            // We don't care if it fails as we have already saved the images.
            try
            {
                await pairTracker.CleanReturnAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}

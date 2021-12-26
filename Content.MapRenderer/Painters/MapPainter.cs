#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpriteComponent = Robust.Server.GameObjects.SpriteComponent;

namespace Content.MapRenderer.Painters
{
    public class MapPainter : ContentIntegrationTest
    {
        public async IAsyncEnumerable<Image> Paint(string map)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var clientOptions = new ClientContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CVars.NetPVS.Name] = "false"
                },
                Pool = false,
                FailureLogLevel = LogLevel.Fatal
            };

            var serverOptions = new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CCVars.GameMap.Name] = map,
                    [CVars.NetPVS.Name] = "false"
                },
                Pool = false,
                FailureLogLevel = LogLevel.Fatal
            };

            var (client, server) = await StartConnectedServerClientPair(clientOptions, serverOptions);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
            await RunTicksSync(client, server, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Console.WriteLine($"Loaded client and server in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();

            var cEntityManager = client.ResolveDependency<IClientEntityManager>();
            var cPlayerManager = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();

            await client.WaitPost(() =>
            {
                if (cEntityManager.TryGetComponent(cPlayerManager.LocalPlayer!.ControlledEntity!, out Robust.Client.GameObjects.SpriteComponent? sprite))
                {
                    sprite.Visible = false;
                }
            });

            var sEntityManager = server.ResolveDependency<IServerEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await server.WaitPost(() =>
            {
                if (sEntityManager.TryGetComponent(sPlayerManager.ServerSessions.Single().AttachedEntity!, out SpriteComponent? sprite))
                {
                    sprite.Visible = false;
                }
            });

            await RunTicksSync(client, server, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sMapManager = server.ResolveDependency<IMapManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new GridPainter(client, server);
            IMapGrid[] grids = null!;

            await server.WaitPost(() =>
            {
                var playerEntity = sPlayerManager.ServerSessions.Single().AttachedEntity;

                if (playerEntity.HasValue)
                {
                    sEntityManager.DeleteEntity(playerEntity.Value);
                }

                grids = sMapManager.GetAllMapGrids(new MapId(1)).ToArray();

                foreach (var grid in grids)
                {
                    grid.WorldRotation = Angle.Zero;
                }
            });

            await RunTicksSync(client, server, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            foreach (var grid in grids)
            {
                var tileXSize = grid.TileSize * TilePainter.TileImageSize;
                var tileYSize = grid.TileSize * TilePainter.TileImageSize;

                var bounds = grid.LocalBounds;

                var left = bounds.Left;
                var right = bounds.Right;
                var top = bounds.Top;
                var bottom = bounds.Bottom;

                var w = (int) Math.Ceiling(right - left) * tileXSize;
                var h = (int) Math.Ceiling(top - bottom) * tileYSize;

                var gridCanvas = new Image<Rgba32>(w, h);

                await server.WaitPost(() =>
                {
                    tilePainter.Run(gridCanvas, grid);
                    entityPainter.Run(gridCanvas, grid);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));
                });

                yield return gridCanvas;
            }

            // We don't care if it fails as we have already saved the images.
            try
            {
                await OneTimeTearDown();
            }
            catch
            {
                // ignored
            }
        }
    }
}

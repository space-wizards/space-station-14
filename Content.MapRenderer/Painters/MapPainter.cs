#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.MapRenderer.Painters
{
    public class MapPainter : ContentIntegrationTest
    {
        public async IAsyncEnumerable<Image> Paint(string map)
        {
            map = map.Substring(10); // Resources/

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var clientOptions = new ClientContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CVars.NetPVS.Name] = "false"
                }
            };

            var serverOptions = new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CCVars.GameMap.Name] = map,
                    [CVars.NetPVS.Name] = "false"
                }
            };

            var (client, server) = await StartConnectedServerClientPair(clientOptions, serverOptions);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
            await RunTicksSync(client, server, 10);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Console.WriteLine($"Loaded client and server in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();

            var cPlayerManager = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();

            await client.WaitPost(() =>
            {
                if (cPlayerManager.LocalPlayer!.ControlledEntity!.TryGetComponent(out Robust.Client.GameObjects.SpriteComponent? sprite))
                {
                    sprite.Visible = false;
                }
            });

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await server.WaitPost(() =>
            {
                if (sPlayerManager.GetAllPlayers().Single().AttachedEntity!.TryGetComponent(out SpriteComponent? sprite))
                {
                    sprite.Visible = false;
                }
            });

            await RunTicksSync(client, server, 2);

            var sMapManager = server.ResolveDependency<IMapManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new EntityPainter(client, server);
            IMapGrid[] grids = null!;

            await server.WaitPost(() =>
            {
                sPlayerManager.GetAllPlayers().Single().AttachedEntity?.Delete();
                grids = sMapManager.GetAllMapGrids(new MapId(1)).ToArray();
            });

            foreach (var grid in grids)
            {
                var tileXSize = 32;
                var tileYSize = 32;

                var bounds = grid.WorldBounds;

                var left = Math.Abs(bounds.Left);
                var right = Math.Abs(bounds.Right);
                var top = Math.Abs(bounds.Top);
                var bottom = Math.Abs(bounds.Bottom);

                var w = (int) Math.Ceiling(left + right) * tileXSize;
                var h = (int) Math.Ceiling(top + bottom) * tileYSize;

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
#pragma warning disable 4014
                TearDown();
#pragma warning restore 4014
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}

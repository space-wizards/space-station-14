using System;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.MapRenderer.Painters;
using Content.Shared;
using Robust.Client;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.MapRenderer
{
    internal class Program : ContentIntegrationTest
    {
        internal static void Main(string[] args)
        {
            new Program().Run().GetAwaiter().GetResult();
        }

        private async Task Run()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var clientOptions = new ClientContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CVars.NetPVS.Name] = "false"
                },
                Mode = DisplayMode.Clyde
            };

            var serverOptions = new ServerContentIntegrationOption
            {
                CVarOverrides =
                {
                    [CCVars.GameMap.Name] = "Maps/saltern.yml",
                    [CVars.NetPVS.Name] = "false"
                }
            };

            var (client, server) = await StartConnectedServerClientPair(clientOptions, serverOptions);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Console.WriteLine($"Loaded client and server in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            var tilePainter = new TilePainter(client, server);
            var wallPainter = new EntityPainter(client, server);

            await server.WaitPost(async () =>
            {
                sPlayerManager.GetAllPlayers().Single().AttachedEntity?.Delete();

                var grids = sMapManager.GetAllMapGrids(new MapId(1));
                var tileXSize = 32;
                var tileYSize = 32;

                foreach (var grid in grids)
                {
                    var bounds = grid.WorldBounds;

                    var left = Math.Abs(bounds.Left);
                    var right = Math.Abs(bounds.Right);
                    var top = Math.Abs(bounds.Top);
                    var bottom = Math.Abs(bounds.Bottom);

                    var w = (int) Math.Ceiling(left + right) * tileXSize;
                    var h = (int) Math.Ceiling(top + bottom) * tileYSize;

                    var gridCanvas = new Image<Rgba32>(w, h);

                    tilePainter.Run(gridCanvas, grid);
                    wallPainter.Run(gridCanvas, grid);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));

                    await gridCanvas.SaveAsync($"C:\\Projects\\C#\\space-station-14\\grid-{grid.Index}.png");
                }
            });

            await TearDown();

            Console.WriteLine($"Saved all map images in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}

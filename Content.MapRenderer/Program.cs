using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.MapRenderer.Painters;
using Content.Shared;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YamlDotNet.RepresentationModel;

namespace Content.MapRenderer
{
    internal class Program : ContentIntegrationTest
    {
        internal static void Main()
        {
            var created = Environment.GetEnvironmentVariable("MAPS_ADDED");
            var modified = Environment.GetEnvironmentVariable("MAPS_MODIFIED");

            var yamlStream = new YamlStream();

            if (created != null)
            {
                yamlStream.Load(new StringReader(created));
            }

            if (modified != null)
            {
                yamlStream.Load(new StringReader(modified));
            }

            var files = new YamlSequenceNode();

            foreach (var doc in yamlStream.Documents)
            {
                var filesModified = (YamlSequenceNode) doc.RootNode;

                foreach (var node in filesModified)
                {
                    files.Add(node);
                }
            }

            var maps = new List<string> {"Resources/Maps/saltern.yml"};

            foreach (var node in files)
            {
                var fileName = node.AsString();

                if (!fileName.StartsWith("Resources/Maps/") ||
                    !fileName.EndsWith("yml"))
                {
                    continue;
                }

                maps.Add(fileName);
            }

            var program = new Program();

            foreach (var map in maps)
            {
                program.Run(map).GetAwaiter().GetResult();
            }
        }

        private async Task Run(string map)
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

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            var tilePainter = new TilePainter(client, server);
            var entityPainter = new EntityPainter(client, server);

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
                    entityPainter.Run(gridCanvas, grid);

                    gridCanvas.Mutate(e => e.Flip(FlipMode.Vertical));

                    var file = new ResourcePath($"MapImages/{map.Substring(5, map.Length - 9)}.png");
                    var directoryPath =
                        $"{Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.Parent!.Parent}/Resources/{file.Directory}";
                    var directory = Directory.CreateDirectory(directoryPath);
                    var path = $"{directory}/{file.Filename}";

                    Console.WriteLine($"Saving {file.Filename} to {path}");

                    await gridCanvas.SaveAsync(path);
                }
            });

            // It fails locally otherwise. We don't care if it fails as we have already saved the images.
            try
            {
                await TearDown();
            }
            catch (InvalidOperationException)
            {
            }

            Console.WriteLine($"Saved map image for {map} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}

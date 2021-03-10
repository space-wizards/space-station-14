using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var files = new YamlSequenceNode();

            if (created != null)
            {
                yamlStream.Load(new StringReader(created));

                var filesCreated = (YamlSequenceNode) yamlStream.Documents[0].RootNode;

                foreach (var node in filesCreated)
                {
                    files.Add(node);
                }
            }

            if (modified != null)
            {
                yamlStream.Load(new StringReader(modified));

                var filesModified = (YamlSequenceNode) yamlStream.Documents[1].RootNode;

                foreach (var node in filesModified)
                {
                    files.Add(node);
                }
            }

            var maps = new List<string>();

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

                    var file = new ResourcePath($"MapImages/{map.Substring(5, map.Length - 9)}.png");
                    var directoryPath =
                        $"{Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!}/Resources/{file.Directory}";
                    var directory = Directory.CreateDirectory(directoryPath);

                    await gridCanvas.SaveAsync($"{directory}/{file.Filename}");
                }
            });

            await TearDown();

            Console.WriteLine($"Saved all map images in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}

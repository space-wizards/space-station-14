#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Painters;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Content.MapRenderer
{
    internal class Program
    {
        private const string MapsAddedEnvKey = "FILES_ADDED";
        private const string MapsModifiedEnvKey = "FILES_MODIFIED";

        private static readonly MapPainter MapPainter = new();

        internal static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Didn't specify any maps to paint! Provide map names (as map prototype names).");
            }

            if (!CommandLineArguments.TryParse(args, out var arguments))
                return;

            await Run(arguments);
        }

        private static async Task Run(CommandLineArguments arguments)
        {

            Console.WriteLine($"Creating images for {arguments.Maps.Count} maps");

            var mapNames = new List<string>();
            foreach (var map in arguments.Maps)
            {
                Console.WriteLine($"Painting map {map}");

                var mapViewerData = new MapViewerData()
                {
                    Id = map,
                    Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(map)
                };

                mapViewerData.ParallaxLayers.Add(LayerGroup.DefaultParallax());
                var directory = Path.Combine(arguments.OutputPath, map);
                Directory.CreateDirectory(directory);

                int i = 0;
                await foreach (var renderedGrid in MapPainter.Paint(map))
                {
                    var grid = renderedGrid.Image;
                    Directory.CreateDirectory(directory);

                    var fileName = Path.GetFileNameWithoutExtension(map);
                    var savePath = $"{directory}{Path.DirectorySeparatorChar}{fileName}-{i}.{arguments.Format.ToString()}";

                    Console.WriteLine($"Writing grid of size {grid.Width}x{grid.Height} to {savePath}");

                    switch (arguments.Format)
                    {
                        case OutputFormat.webp:
                            var encoder = new WebpEncoder
                            {
                                Method = WebpEncodingMethod.BestQuality,
                                FileFormat = WebpFileFormatType.Lossless,
                                TransparentColorMode = WebpTransparentColorMode.Preserve
                            };

                            await grid.SaveAsync(savePath, encoder);
                            break;

                        default:
                        case OutputFormat.png:
                            await grid.SaveAsPngAsync(savePath);
                            break;
                    }

                    grid.Dispose();

                    mapViewerData.Grids.Add(new GridLayer(renderedGrid,  Path.Combine(map, Path.GetFileName(savePath))));

                    mapNames.Add(fileName);
                    i++;
                }

                if (arguments.ExportViewerJson)
                {
                    var json = JsonConvert.SerializeObject(mapViewerData);
                    await File.WriteAllTextAsync(Path.Combine(arguments.OutputPath, map, "map.json"), json);
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Created {arguments.Maps.Count} map images.");
        }
    }
}

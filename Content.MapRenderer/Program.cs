#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Painters;
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

                int i = 0;
                await foreach (var renderedGrid in MapPainter.Paint(map))
                {
                    var grid = renderedGrid.Image;
                    var directory = DirectoryExtensions.MapImages().FullName;
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

                    mapNames.Add(fileName);
                    i++;
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Created {arguments.Maps.Count} map images.");
        }
    }
}

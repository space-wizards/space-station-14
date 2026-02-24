#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.MapRenderer.Painters;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Content.MapRenderer
{
    internal sealed class Program
    {
        private const string NoMapsChosenMessage = "No maps were chosen";
        private static readonly Func<string, string> ChosenMapIdNotIntMessage = id => $"The chosen id is not a valid integer: {id}";
        private static readonly Func<int, string> NoMapFoundWithIdMessage = id => $"No map found with chosen id: {id}";

        internal static async Task Main(string[] args)
        {
            if (!CommandLineArguments.TryParse(args, out var arguments))
                return;

            var testContext = new ExternalTestContext("Content.MapRenderer", Console.Out);

            PoolManager.Startup();
            if (arguments.Maps.Count == 0)
            {
                Console.WriteLine("Didn't specify any maps to paint! Loading the map list...");

                await using var pair = await PoolManager.GetServerClient(testContext: testContext);
                var mapIds = pair.Server
                    .ResolveDependency<IPrototypeManager>()
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Where(map => !pair.IsTestPrototype(map))
                    .Select(map => map.ID)
                    .ToArray();

                Array.Sort(mapIds);

                Console.WriteLine("Map List");
                Console.WriteLine(string.Join('\n', mapIds.Select((id, i) => $"{i,3}: {id}")));
                Console.WriteLine("Select one, multiple separated by commas or \"all\":");
                Console.Write("> ");
                var input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine(NoMapsChosenMessage);
                    return;
                }

                var selectedIds = new List<int>();
                if (input is "all" or "\"all\"")
                {
                    selectedIds = Enumerable.Range(0, mapIds.Length).ToList();
                }
                else
                {
                    var inputArray = input.Split(',');
                    if (inputArray.Length == 0)
                    {
                        Console.WriteLine(NoMapsChosenMessage);
                        return;
                    }

                    foreach (var idString in inputArray)
                    {
                        if (!int.TryParse(idString.Trim(), out var id))
                        {
                            Console.WriteLine(ChosenMapIdNotIntMessage(idString));
                            return;
                        }

                        selectedIds.Add(id);
                    }
                }

                var selectedMapPrototypes = new List<string>();
                foreach (var id in selectedIds)
                {
                    if (id < 0 || id >= mapIds.Length)
                    {
                        Console.WriteLine(NoMapFoundWithIdMessage(id));
                        return;
                    }

                    selectedMapPrototypes.Add(mapIds[id]);
                }

                arguments.Maps.AddRange(selectedMapPrototypes);

                if (selectedMapPrototypes.Count == 0)
                {
                    Console.WriteLine(NoMapsChosenMessage);
                    return;
                }

                Console.WriteLine($"Selected maps: {string.Join(", ", selectedMapPrototypes)}");
            }

            var maps = new List<RenderMap>();

            if (arguments.ArgumentsAreFileNames)
            {
                Console.WriteLine("Retrieving maps by file names...");

                //
                // Handle legacy command line processing:
                // Ideally, people pass file names that are relative to the process working directory.
                // i.e. regular command-line behavior.
                //
                // However, the map renderer was originally written to only handle gameMap prototypes,
                // so it would actually go through the list of prototypes and match file name arguments
                // via a *very* coarse check.
                //
                // So if we have any input filenames that don't exist... we run the old behavior.
                // Yes by the way this means a typo means spinning up an entire integration pool pair
                // before the map renderer can report a proper failure.
                //
                // Note that this legacy processing is very important! The map server currently relies on it,
                // because it wants to work with file names, but we *need* to resolve the input to a prototype
                // to properly export viewer JSON data.
                //

                var lookupPrototypeFiles = new List<string>();

                foreach (var map in arguments.Maps)
                {
                    if (File.Exists(map))
                    {
                        maps.Add(new RenderMapFile { FileName = map });
                    }
                    else
                    {
                        lookupPrototypeFiles.Add(map);
                    }
                }

                if (lookupPrototypeFiles.Count > 0)
                {
                    Console.Write($"Following map files did not exist on disk directly, searching through prototypes: {string.Join(", ", lookupPrototypeFiles)}");

                    await using var pair = await PoolManager.GetServerClient();
                    var mapPrototypes = pair.Server
                        .ResolveDependency<IPrototypeManager>()
                        .EnumeratePrototypes<GameMapPrototype>()
                        .ToArray();

                    foreach (var toFind in lookupPrototypeFiles)
                    {
                        foreach (var mapPrototype in mapPrototypes)
                        {
                            if (mapPrototype.MapPath.Filename == toFind)
                            {
                                maps.Add(new RenderMapPrototype { Prototype = mapPrototype, });
                                Console.WriteLine($"Found matching map prototype: {mapPrototype.MapName}");
                                goto found;
                            }
                        }

                        await Console.Error.WriteLineAsync($"Found no map prototype for file '{toFind}'!");

                        found: ;
                    }
                }
            }
            else
            {
                foreach (var map in arguments.Maps)
                {
                    maps.Add(new RenderMapPrototype { Prototype = map });
                }
            }

            await Run(arguments, maps, testContext);
            PoolManager.Shutdown();
        }

        private static async Task Run(
            CommandLineArguments arguments,
            List<RenderMap> toRender,
            ExternalTestContext testContext)
        {
            Console.WriteLine($"Creating images for {toRender.Count} maps");

            var parallaxOutput = arguments.OutputParallax ? new ParallaxOutput(arguments.OutputPath) : null;

            var mapNames = new List<string>();
            foreach (var map in toRender)
            {
                Console.WriteLine($"Painting map {map}");

                await using var painter = new MapPainter(map, testContext);
                await painter.Initialize();
                await painter.SetupView(showMarkers: arguments.ShowMarkers);

                var mapViewerData = await painter.GenerateMapViewerData(parallaxOutput);

                var mapShort = map.ShortName;
                var directory = Path.Combine(arguments.OutputPath, mapShort);

                mapNames.Add(mapShort);

                var i = 0;
                try
                {
                    await foreach (var renderedGrid in painter.Paint())
                    {
                        var grid = renderedGrid.Image;
                        Directory.CreateDirectory(directory);

                        var savePath = $"{directory}{Path.DirectorySeparatorChar}{mapShort}-{i}.{arguments.Format}";

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

                        mapViewerData.Grids.Add(new GridLayer(renderedGrid, Path.Combine(mapShort, Path.GetFileName(savePath))));
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Painting map {map} failed due to an internal exception:");
                    Console.WriteLine(ex);
                    continue;
                }

                if (arguments.ExportViewerJson)
                {
                    var json = JsonSerializer.Serialize(mapViewerData);
                    await File.WriteAllTextAsync(Path.Combine(directory, "map.json"), json);
                }

                try
                {
                    await painter.CleanReturnAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while shutting down painter: {e}");
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Processed {arguments.Maps.Count} maps.");
            Console.WriteLine($"It's now safe to manually exit the process (automatic exit in a few moments...)");
        }
    }
}

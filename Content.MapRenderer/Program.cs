#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Painters;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using YamlDotNet.RepresentationModel;

namespace Content.MapRenderer
{
    internal class Program
    {
        private const string MapsAddedEnvKey = "FILES_ADDED";
        private const string MapsModifiedEnvKey = "FILES_MODIFIED";
        private const string GitHubRepositoryEnvKey = "GITHUB_REPOSITORY";
        private const string PrNumberEnvKey = "PR_NUMBER";

        private static readonly MapPainter MapPainter = new();

#pragma warning disable CA1825
        private static readonly string[] ForceRender =
        {
            // "Resources/Maps/saltern.yml"
        };
#pragma warning restore CA1825

        internal static void Main()
        {
            new Program().Run().Wait();
        }

        private async Task Run()
        {
            var created = Environment.GetEnvironmentVariable(MapsAddedEnvKey);
            var modified = Environment.GetEnvironmentVariable(MapsModifiedEnvKey);

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

            var maps = new List<string>(ForceRender);

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

            Console.WriteLine($"Creating images for {maps.Count} maps");

            var mapNames = new List<string>();
            foreach (var map in maps)
            {
                Console.WriteLine($"Painting map {map}");

                await foreach (var grid in MapPainter.Paint(map))
                {
                    var fileName = Path.GetFileNameWithoutExtension(map);
                    var savePath = $"{DirectoryExtensions.MapImages().FullName}{Path.DirectorySeparatorChar}{fileName}.png";

                    Console.WriteLine($"Writing grid of size {grid.Width}x{grid.Height} to {savePath}");

                    await grid.SaveAsPngAsync(savePath);
                    grid.Dispose();

                    mapNames.Add(fileName);
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Created {maps.Count} map images.");

            // var repo = EnvironmentExtensions.GetVariableOrThrow(GitHubRepositoryEnvKey);
            // var prNumber = int.Parse(EnvironmentExtensions.GetVariableOrThrow(PrNumberEnvKey));
            // var writer = new GitHubClient(repo);
            // var message = writer.Write(links);
            //
            // writer.Send(prNumber, message);
        }
    }
}

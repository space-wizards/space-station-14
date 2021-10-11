#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.GitHub;
using Content.MapRenderer.Imgur.Client;
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


            Console.WriteLine($"Creating images for {maps.Count} maps");

            foreach (var map in maps)
            {
                Console.WriteLine($"Painting map {map}");

                await foreach (var grid in MapPainter.Paint(map))
                {
                    var savePath = DirectoryExtensions.MapImages().FullName;

                    Console.WriteLine($"Writing grid of size {grid.Width}x{grid.Height} to {savePath}");

                    await grid.SaveAsPngAsync(savePath);
                    grid.Dispose();
                }
            }

            // var repo = EnvironmentExtensions.GetVariableOrThrow(GitHubRepositoryEnvKey);
            // var prNumber = int.Parse(EnvironmentExtensions.GetVariableOrThrow(PrNumberEnvKey));
            // var writer = new GitHubClient(repo);
            // var message = writer.Write(links);
            //
            // writer.Send(prNumber, message);
        }
    }
}

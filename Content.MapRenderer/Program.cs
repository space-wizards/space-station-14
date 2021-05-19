#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.GitHub;
using Content.MapRenderer.Imgur.Client;
using Content.MapRenderer.Imgur.Response;
using Content.MapRenderer.Painters;
using Robust.Shared.Timing;
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
        private static readonly ImgurClient ImgurClient = new();

        internal static void Main()
        {
            new Program().Run().ConfigureAwait(false);
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

            var images = new List<ImgurUploadResponse>();

            foreach (var map in maps)
            {
                await foreach (var grid in MapPainter.Paint(map))
                {
                    var image = await ImgurClient.Upload(grid);
                    images.Add(image);

                    grid.Dispose();
                }
            }

            var repo = EnvironmentExtensions.GetVariableOrThrow(GitHubRepositoryEnvKey);
            var prNumber = int.Parse(EnvironmentExtensions.GetVariableOrThrow(PrNumberEnvKey));
            var writer = new GitHubClient(repo);
            var message = writer.Write(images.Select(i => i.Link));

            writer.Send(prNumber, message);
        }

        private async void Save(Image image, string to)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            var file = new ResourcePath($"MapImages/{to.Substring(5, to.Length - 9)}.png");
            var mapImages = DirectoryExtensions.MapImages();
            var path = $"{mapImages.FullName}/{file.Filename}";

            Console.WriteLine($"Saving {file.Filename} to {path}");

            await image.SaveAsync(path);

            Console.WriteLine($"Saved map image for {to} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }
    }
}

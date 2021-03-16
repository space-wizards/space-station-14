#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using YamlDotNet.RepresentationModel;

namespace Content.MapRenderer
{
    internal class Program
    {
        private static readonly HttpClient HttpClient = new();
        private static readonly Painter Painter = new();

        internal static void Main()
        {
            new Program().Run();
        }

        private async void Run()
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
                await foreach (var grid in Painter.Paint(map))
                {
                    program.Save(grid, map);
                }
            }

            WriteComment();
        }

        private async void Save(Image image, string to)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            var file = new ResourcePath($"MapImages/{to.Substring(5, to.Length - 9)}.png");
            var directoryPath = $"{GetRepositoryRoot()}/Resources/{file.Directory}";
            var directory = Directory.CreateDirectory(directoryPath);
            var path = $"{directory}/{file.Filename}";

            Console.WriteLine($"Saving {file.Filename} to {path}");

            await image.SaveAsync(path);

            Console.WriteLine($"Saved map image for {to} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private void Upload(Image image)
        {
            var request = WebRequest.Create("https://api.imgur.com/3/upload");
            request.Method = "POST";

            byte[] data;

            using (var stream = new MemoryStream())
            {
                image.SaveAsPng(stream);

                data = stream.ToArray();
            }

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            var dataStream = request.GetRequestStream();
            dataStream.Write(data, 0, data.Length);
            dataStream.Close();

            var response = request.GetResponse();

            using (dataStream = response.GetResponseStream())
            {
                var reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
            }

            response.Close();
        }

        private void WriteComment()
        {

        }

        private DirectoryInfo GetRepositoryRoot()
        {
            // space-station-14/bin/Content.MapRenderer/Content.MapRenderer.dll
            var currentLocation = Assembly.GetExecutingAssembly().Location;

            // space-station-14/
            return Directory.GetParent(currentLocation)!.Parent!.Parent!;
        }
    }
}

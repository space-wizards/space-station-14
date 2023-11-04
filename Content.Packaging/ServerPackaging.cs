using System.Diagnostics;
using System.IO.Compression;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Packaging.Utility;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Timing;
using YamlDotNet.RepresentationModel;

namespace Content.Packaging;

public static class ServerPackaging
{
    private static readonly string[] _platforms =
    {
        "win-x64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "win-x86",
        "linux-x86",
        "linux-arm"
    };

    private static async Task BuildPlatform(string platform, bool skipBuild)
    {
        if (!skipBuild)
        {
            await ProcessHelpers.RunCheck(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "build",
                    Path.Combine("Content.Server", "Content.Server.csproj"),
                    "-c", "Release",
                    "--nologo",
                    "/v:m",
                    $"/p:TargetOs={platform}",
                    "/t:Rebuild",
                    "/p:FullRelease=true",
                    "/m"
                }
            });
        }
    }

    public static async Task PackageServer(bool skipBuild, bool hybridAcz, IPackageLogger logger)
    {
        logger.Info("Building server...");

        foreach (var platform in _platforms)
        {
            await BuildPlatform(platform, skipBuild);
        }

        logger.Info("Packaging server...");

        var sw = RStopwatch.StartNew();
        {
            await using var zipFile =
                File.Open(Path.Combine("release", "SS14.Server.zip"), FileMode.Create, FileAccess.ReadWrite);
            using var zip = new ZipArchive(zipFile, ZipArchiveMode.Update);
            var writer = new AssetPassZipWriter(zip);

            await WriteServerResources("", writer, logger, default);

            await writer.FinishedTask;
        }

        logger.Info($"Finished packaging server in {sw.Elapsed}");
    }

    public static async Task WriteServerResources(
        string contentDir,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var graph = new RobustClientAssetGraph();
        var passes = graph.AllPasses.ToList();
        passes.Add(new AudioMetadataAssetPass());

        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));
        passes.Add(pass);

        AssetGraph.CalculateGraph(passes, logger);

        var inputPass = graph.Input;

        await RobustSharedPackaging.WriteContentAssemblies(
            inputPass,
            contentDir,
            "Content.Server",
            new[] { "Content.Server", "Content.Shared", "Content.Shared.Database" },
            cancel);


        // TODO:
        // - Need an AssetPass to strip out audio and dump it into a metadata yml
        // Then validate python files are equivalent.
        // TODO: Copy ignore set from python
        // TODO: Copy copy set from python.
        await RobustSharedPackaging.DoResourceCopy(Path.Combine(contentDir, "Resources"), pass, ignoreSet, cancel);

        inputPass.InjectFinished();
    }

    /// <summary>
    /// Strips out audio files and writes them to a metadata .yml
    /// </summary>
    private sealed class AudioMetadataAssetPass : AssetPass
    {
        private string[] _audioExtensions = new[]
        {
            ".ogg",
            ".wav",
        };

        private List<AudioMetadataPrototype> _audioMetadata = new();

        private string _metadataPath = "audio_metadata.yml";

        protected override AssetFileAcceptResult AcceptFile(AssetFile file)
        {
            var ext = Path.GetExtension(file.Path);

            if (!_audioExtensions.Contains(ext))
            {
                return AssetFileAcceptResult.Pass;
            }

            var updatedName = file.Path.Replace("/", "_");
            TimeSpan length;

            if (ext == ".ogg")
            {
                using var stream = file.Open();
                using var vorbis = new NVorbis.VorbisReader(stream, false);
                length = vorbis.TotalTime;
            }
            else if (ext == ".wav")
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException($"No audio metadata processing implemented for {ext}");
            }

            _audioMetadata.Add(new AudioMetadataPrototype()
            {
                ID = updatedName,
                Length = length,
            });

            return AssetFileAcceptResult.Consumed;
        }

        protected override void AcceptFinished()
        {
            if (_audioMetadata.Count == 0)
                return;

            var serManager = new SerializationManager();

            // ReSharper disable once InconsistentlySynchronizedField
            var root = new YamlSequenceNode();
            var document = new YamlDocument(root);

            foreach (var prototype in _audioMetadata)
            {
                var weh = serManager.WriteValue(prototype);
                var jaml = weh.ToYamlNode();
                root.Add(jaml);
            }

            RunJob(() =>
            {
                // Console.WriteLine($"Packing RSI: {key}");
                var memory = new MemoryStream();
                var streamWriter = new StreamWriter(memory);
                var yamlStream = new YamlStream(document);
                yamlStream.Save(streamWriter);

                var result = new AssetFileMemory(_metadataPath, memory.ToArray());
                SendFile(result);
            });
        }
    }
}

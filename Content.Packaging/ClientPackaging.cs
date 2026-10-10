using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Packaging.Utility;
using Robust.Shared.Timing;

namespace Content.Packaging;

public static class ClientPackaging
{
    private static readonly List<PlatformReg> Platforms =
    [
        new("win-x64", "Windows"),
        new("win-arm64", "Windows"),
        new("linux-x64", "Linux"),
        new("linux-arm64", "Linux"),
        new("osx-x64", "MacOS"),
        new("osx-arm64", "MacOS"),
    ];

    private static readonly HashSet<string> BinSkipFolders =
    [
        "cs",
        "de",
        "es",
        "fr",
        "it",
        "ja",
        "ko",
        "pl",
        "pt-BR",
        "ru",
        "tr",
        "zh-Hans",
        "zh-Hant",
    ];

    /// <summary>
    /// Be advised this can be called from server packaging during a HybridACZ build.
    /// </summary>
    public static async Task PackageClient(bool skipBuild, bool logBuild, string configuration, IPackageLogger logger)
    {
        var packagePath = Path.GetFullPath(Path.Combine("release", "SS14.Client.zip"));

        logger.Info("Building client...");

        if (!skipBuild)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "build",
                    Path.Combine("Content.Client", "Content.Client.csproj"),
                    "-c", configuration,
                    "--nologo",
                    "/v:m",
                    "/p:FullRelease=true",
                    "/m",
                },
            };

            if (logBuild)
            {
                var binlogPath = Path.GetFullPath(Path.Combine("release", "client.binlog"));
                logger.Info($"Client build log: {binlogPath}");
                startInfo.ArgumentList.Add($"/bl:{binlogPath}");
                startInfo.ArgumentList.Add("/p:ReportAnalyzer=true");
            }

            await ProcessHelpers.RunCheck(startInfo);
        }

        logger.Info("Packaging client...");
        logger.Info($"Client package output: {packagePath}");

        var sw = RStopwatch.StartNew();
        {
            await using var zipFile = File.Open(packagePath, FileMode.Create, FileAccess.ReadWrite);
            await using var zip = new ZipArchive(zipFile, ZipArchiveMode.Create);
            var writer = new AssetPassZipWriter(zip);

            await WriteResources("", writer, logger, default);
            await writer.FinishedTask;
        }

        logger.Info($"Finished packaging client in {sw.Elapsed}: {packagePath}");
    }

    public static async Task PackageStandaloneClient(
        bool skipBuild,
        bool logBuild,
        string configuration,
        IPackageLogger logger,
        List<string>? platforms = null)
    {
        platforms ??= [GetCurrentRid()];

        var selectedPlatforms = Platforms
            .Where(o => platforms.Contains(o.Rid))
            .ToList();

        if (selectedPlatforms.Count != platforms.Count)
        {
            var supportedPlatforms = string.Join(", ", Platforms.Select(o => o.Rid));
            throw new InvalidOperationException($"Invalid standalone client platform. Supported platforms: {supportedPlatforms}");
        }

        Dictionary<string, string> contentBinDirs;
        if (skipBuild)
        {
            contentBinDirs = selectedPlatforms
                .Select(o => o.TargetOs)
                .Distinct()
                .ToDictionary(o => o, _ => "Content.Client");
        }
        else
        {
            var targetOperatingSystems = selectedPlatforms
                .Select(o => o.TargetOs)
                .Distinct()
                .ToList();

            foreach (var targetOs in targetOperatingSystems)
            {
                await BuildContentClient(targetOs, logBuild, configuration, logger);
            }

            foreach (var platform in selectedPlatforms)
            {
                await PublishRobustClient(platform.Rid, platform.TargetOs, configuration, logger);
            }

            contentBinDirs = targetOperatingSystems
                .ToDictionary(o => o, GetContentClientBinDir);
        }

        await Task.WhenAll(selectedPlatforms.Select(platform =>
            PackageStandalonePlatform(platform, contentBinDirs[platform.TargetOs], logger)));
    }

    public static async Task WriteResources(
        string contentDir,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        await WriteResources(contentDir, "Content.Client", pass, logger, cancel);
    }

    private static async Task BuildContentClient(
        string targetOs,
        bool logBuild,
        string configuration,
        IPackageLogger logger)
    {
        logger.Info($"Building content client for {targetOs}...");
        logger.Info($"Content client build output for {targetOs}: {GetContentClientOutputPath(targetOs)}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "build",
                Path.Combine("Content.Client", "Content.Client.csproj"),
                "-c", configuration,
                "--nologo",
                "/v:m",
                $"/p:TargetOs={targetOs}",
                $"/p:OutputPath={GetContentClientOutputPath(targetOs)}",
                "/p:FullRelease=true",
                "/m",
            },
        };

        if (logBuild)
        {
            var binlogPath = Path.GetFullPath(Path.Combine("release", $"client-content-{targetOs}.binlog"));
            logger.Info($"Content client build log for {targetOs}: {binlogPath}");
            startInfo.ArgumentList.Add($"/bl:{binlogPath}");
            startInfo.ArgumentList.Add("/p:ReportAnalyzer=true");
        }

        await ProcessHelpers.RunCheck(startInfo);
    }

    private static async Task PublishRobustClient(string runtime, string targetOs, string configuration, IPackageLogger logger)
    {
        var publishPath = Path.GetFullPath(Path.Combine("RobustToolbox", "bin", "Client", runtime, "publish"));
        logger.Info($"Robust.Client publish output for {runtime}: {publishPath}");

        await ProcessHelpers.RunCheck(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "publish",
                "--runtime", runtime,
                "--no-self-contained",
                "-c", configuration,
                $"/p:TargetOS={targetOs}",
                "/p:FullRelease=True",
                "/p:UseAppHost=True",
                "/m",
                "RobustToolbox/Robust.Client/Robust.Client.csproj",
            },
        });
    }

    private static async Task PackageStandalonePlatform(PlatformReg platform, string contentBinDir, IPackageLogger logger)
    {
        var packagePath = Path.GetFullPath(Path.Combine("release", $"SS14.Client_{platform.Rid}.zip"));

        logger.Info($"Packaging standalone {platform.Rid} client...");
        logger.Info($"Standalone client package output for {platform.Rid}: {packagePath}");

        var sw = RStopwatch.StartNew();
        {
            await using var zipFile = File.Open(packagePath, FileMode.Create, FileAccess.ReadWrite);
            await using var zip = new ZipArchive(zipFile, ZipArchiveMode.Create);
            var writer = new AssetPassZipWriter(zip);

            await RobustSharedPackaging.DoResourceCopy(
                Path.Combine("RobustToolbox", "bin", "Client", platform.Rid, "publish"),
                writer,
                BinSkipFolders,
                cancel: default);

            await WriteStandaloneResources(contentBinDir, writer, logger, default);
            await writer.FinishedTask;
        }

        logger.Info($"Finished packaging standalone {platform.Rid} client in {sw.Elapsed}: {packagePath}");
    }

    private static async Task WriteResources(
        string contentDir,
        string contentBinDir,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var graph = new RobustClientAssetGraph();
        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        var dropSvgPass = new AssetPassFilterDrop(f => f.Path.EndsWith(".svg"))
        {
            Name = "DropSvgPass",
        };
        dropSvgPass.AddDependency(graph.Input).AddBefore(graph.PresetPasses);

        AssetGraph.CalculateGraph([pass, dropSvgPass, ..graph.AllPasses], logger);

        var inputPass = graph.Input;

        await RobustSharedPackaging.WriteContentAssemblies(
            inputPass,
            contentDir,
            contentBinDir,
            new[] { "Content.Client", "Content.Shared", "Content.Shared.Database" },
            cancel: cancel);

        await RobustClientPackaging.WriteClientResources(
            contentDir,
            inputPass,
            SharedPackaging.AdditionalIgnoredResources,
            cancel);

        inputPass.InjectFinished();
    }

    private static async Task WriteStandaloneResources(
        string contentBinDir,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        await RobustSharedPackaging.DoResourceCopy(
            Path.Combine("RobustToolbox", "Resources"),
            pass,
            RobustSharedPackaging.SharedIgnoredResources,
            targetDir: "Resources",
            cancel: cancel);

        await RobustSharedPackaging.WriteContentAssemblies(
            pass,
            "",
            contentBinDir,
            new[] { "Content.Client", "Content.Shared", "Content.Shared.Database" },
            targetDir: "Resources/Assemblies",
            cancel: cancel);

        await WriteStandaloneClientResources(pass, logger, cancel);
    }

    private static async Task WriteStandaloneClientResources(
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var graph = new RobustClientAssetGraph();

        var prefixPass = new AssetPassPrefix("Resources/")
        {
            Name = "StandaloneClientResourcePrefix",
        };
        prefixPass.AddDependency(graph.Output);
        pass.AddDependency(prefixPass);

        var dropSvgPass = new AssetPassFilterDrop(f => f.Path.EndsWith(".svg"))
        {
            Name = "DropSvgPass",
        };
        dropSvgPass.AddDependency(graph.Input).AddBefore(graph.PresetPasses);

        AssetGraph.CalculateGraph([pass, prefixPass, dropSvgPass, ..graph.AllPasses], logger);

        var inputPass = graph.Input;
        await RobustClientPackaging.WriteClientResources(
            "",
            inputPass,
            SharedPackaging.AdditionalIgnoredResources,
            cancel);

        inputPass.InjectFinished();
    }

    private static string GetContentClientBinDir(string targetOs)
    {
        return Path.Combine("Packaging", "Content.Client", targetOs);
    }

    private static string GetContentClientOutputPath(string targetOs)
    {
        return EnsureTrailingDirectorySeparator(Path.GetFullPath(Path.Combine("bin", GetContentClientBinDir(targetOs))));
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        return Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string GetCurrentRid()
    {
        var rid = RuntimeInformation.RuntimeIdentifier;
        return Platforms.Any(o => o.Rid == rid)
            ? rid
            : throw new InvalidOperationException($"Unsupported current runtime identifier: {rid}");
    }

    private readonly record struct PlatformReg(string Rid, string TargetOs);
}

using System.Diagnostics;
using System.IO.Compression;
using Content.Packaging;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Packaging.Utility;
using Robust.Shared.Timing;

IPackageLogger logger = new PackageLoggerConsole();

logger.Info("Clearing release/ directory");
Directory.CreateDirectory("release");

var skipBuild = args.Contains("--skip-build");

if (!skipBuild)
    WipeBin();

await Build(skipBuild);

async Task Build(bool skipBuild)
{
    logger.Info("Building project...");

    if (!skipBuild)
    {
        await ProcessHelpers.RunCheck(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "build",
                Path.Combine("Content.Client", "Content.Client.csproj"),
                "-c", "Release",
                "--nologo",
                "/v:m",
                "/t:Rebuild",
                "/p:FullRelease=true",
                "/m"
            }
        });
    }

    logger.Info("Packaging client...");

    var sw = RStopwatch.StartNew();

    {
        using var zipFile =
            File.Open(Path.Combine("release", "SS14.Client.zip"), FileMode.Create, FileAccess.ReadWrite);
        using var zip = new ZipArchive(zipFile, ZipArchiveMode.Update);
        var writer = new AssetPassZipWriter(zip);

        await ContentPackaging.WriteResources("", writer, logger, default);

        await writer.FinishedTask;
    }

    logger.Info($"Finished packaging in {sw.Elapsed}");
}


void WipeBin()
{
    logger.Info("Clearing old build artifacts (if any)...");

    Directory.Delete("bin", recursive: true);
}

using Content.Packaging;
using Robust.Packaging;

IPackageLogger logger = new PackageLoggerConsole();

logger.Info("Clearing release/ directory");
Directory.CreateDirectory("release");

var skipBuild = args.Contains("--skip-build");

if (!skipBuild)
    WipeBin();

await ServerPackaging.PackageServer(skipBuild, logger);
await ContentPackaging.PackageClient(skipBuild, logger);


void WipeBin()
{
    logger.Info("Clearing old build artifacts (if any)...");

    if (Directory.Exists("bin"))
        Directory.Delete("bin", recursive: true);
}

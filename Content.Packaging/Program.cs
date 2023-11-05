using Content.Packaging;
using Robust.Packaging;

IPackageLogger logger = new PackageLoggerConsole();

WipeRelease();

var skipBuild = args.Contains("--skip-build");

if (!skipBuild)
    WipeBin();

await ServerPackaging.PackageServer(skipBuild, true, logger);
// await ContentPackaging.PackageClient(skipBuild, logger);


void WipeBin()
{
    logger.Info("Clearing old build artifacts (if any)...");

    if (Directory.Exists("bin"))
        Directory.Delete("bin", recursive: true);
}

void WipeRelease()
{
    if (Directory.Exists("release"))
    {
        logger.Info("Cleaning old release packages (release/)...");
        Directory.Delete("release", recursive: true);
    }

    Directory.CreateDirectory("release");
}

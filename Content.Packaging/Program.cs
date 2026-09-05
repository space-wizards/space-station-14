using Content.Packaging;
using Robust.Packaging;

IPackageLogger logger = new PackageLoggerConsole();

if (!CommandLineArgs.TryParse(args, out var parsed, out var help))
{
    if (!help)
        logger.Error("Unable to parse args, aborting.");

    return help ? 0 : 1;
}

var contentRoot = Path.GetFullPath(parsed.ContentRoot);
if (!IsContentRoot(contentRoot))
{
    logger.Error(
        $"Invalid content root '{contentRoot}'. Expected to find Content.Server/Content.Server.csproj and RobustToolbox/.");
    return 1;
}

Directory.SetCurrentDirectory(contentRoot);
logger.Info($"Packaging content root: {contentRoot}");

if (parsed.WipeRelease)
    WipeRelease();
else
{
    // Ensure the release directory exists. Otherwise, the packaging will fail.
    Directory.CreateDirectory("release");
}

logger.Info($"Package output directory: {Path.GetFullPath("release")}");

if (!parsed.SkipBuild)
    WipeBin();

if (parsed.Client)
{
    if (parsed.Standalone)
        await ClientPackaging.PackageStandaloneClient(parsed.SkipBuild, parsed.LogBuild, parsed.Configuration, logger, parsed.Platforms);
    else
        await ClientPackaging.PackageClient(parsed.SkipBuild, parsed.LogBuild, parsed.Configuration, logger);
}
else
{
    await ServerPackaging.PackageServer(parsed.SkipBuild, parsed.HybridAcz, parsed.LogBuild, logger, parsed.Configuration, parsed.Platforms);
}

return 0;

static bool IsContentRoot(string path)
{
    return File.Exists(Path.Combine(path, "Content.Server", "Content.Server.csproj"))
        && Directory.Exists(Path.Combine(path, "RobustToolbox"));
}

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

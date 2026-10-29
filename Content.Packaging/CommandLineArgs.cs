using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Content.Packaging;

public sealed class CommandLineArgs
{
    // PJB forgib me

    /// <summary>
    /// Generate client or server.
    /// </summary>
    public bool Client { get; set; }

    /// <summary>
    /// Should we also build the relevant project.
    /// </summary>
    public bool SkipBuild { get; set; }

    /// <summary>
    /// Should we wipe the release folder or ignore it.
    /// </summary>
    public bool WipeRelease { get; set; }

    /// <summary>
    /// Platforms for server packaging.
    /// </summary>
    public List<string>? Platforms { get; set; }

    /// <summary>
    /// Use HybridACZ for server packaging.
    /// </summary>
    public bool HybridAcz { get; set; }

    /// <summary>
    /// Build a standalone runnable client package instead of a client content package.
    /// </summary>
    public bool Standalone { get; set; }

    /// <summary>
    /// Configuration used for when packaging the server. (Release, Debug, Tools)
    /// </summary>
    public string Configuration { get; set; }

    /// <summary>
    /// Log builds with MSBuild binlog. Logs get saved to release/
    /// </summary>
    public bool LogBuild { get; set; }

    /// <summary>
    /// Root directory of the content repository to package.
    /// </summary>
    public string ContentRoot { get; set; }

    /// <summary>
    /// Whether help was requested.
    /// </summary>
    public bool Help { get; set; }

    // CommandLineArgs, 3rd of her name.
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsed, out bool help)
    {
        parsed = null;
        help = false;

        if (args.Contains("--help"))
        {
            PrintHelp();
            help = true;
            return false;
        }

        bool? client = null;
        var skipBuild = false;
        var wipeRelease = true;
        var hybridAcz = false;
        var standalone = false;
        var logBuild = false;
        var configuration = "Release";
        var contentRoot = Directory.GetCurrentDirectory();
        List<string>? platforms = null;

        using var enumerator = args.GetEnumerator();
        var i = -1;

        while (enumerator.MoveNext())
        {
            i++;
            var arg = enumerator.Current;
            if (i == 0)
            {
                if (arg == "client")
                {
                    client = true;
                }
                else if (arg == "server")
                {
                    client = false;
                }
                else
                {
                    return false;
                }

                continue;
            }

            if (arg == "--skip-build")
            {
                skipBuild = true;
            }
            else if (arg == "--no-wipe-release")
            {
                wipeRelease = false;
            }
            else if (arg == "--hybrid-acz")
            {
                hybridAcz = true;
            }
            else if (arg == "--standalone")
            {
                standalone = true;
            }
            else if (arg == "--log-build")
            {
                logBuild = true;
            }
            else if (arg == "--platform")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No platform provided");
                    return false;
                }

                platforms ??= new List<string>();
                platforms.Add(enumerator.Current == "current"
                    ? RuntimeInformation.RuntimeIdentifier
                    : enumerator.Current);
            }
            else if (arg == "--configuration")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No configuration provided");
                    return false;
                }

                configuration = enumerator.Current;
            }
            else if (arg == "--content-root")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No content root provided");
                    return false;
                }

                contentRoot = enumerator.Current;
            }
            else
            {
                Console.WriteLine("Unknown argument: {0}", arg);
                return false;
            }
        }

        if (client == null)
        {
            Console.WriteLine("Client / server packaging unspecified.");
            return false;
        }

        parsed = new CommandLineArgs(client.Value, skipBuild, wipeRelease, hybridAcz, standalone, logBuild, platforms, configuration, contentRoot, help);
        return true;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Usage: Content.Packaging [client/server] [options]

Options:
  --skip-build          Should we skip building the project and use what's already there.
  --no-wipe-release     Don't wipe the release folder before creating files.
  --hybrid-acz          Use HybridACZ for server builds.
  --standalone          Build a runnable standalone client package.
  --platform            Platform for server or standalone client builds. Use 'current' for this machine.
  --configuration       Configuration to use for building the server (Release, Debug, Tools). Default is Release.
  --content-root        Root directory of the content repository to package. Defaults to the current directory.
  --log-build           Log builds with MSBuild binlog. Logs get saved to release/
");
    }

    private CommandLineArgs(
        bool client,
        bool skipBuild,
        bool wipeRelease,
        bool hybridAcz,
        bool standalone,
        bool logBuild,
        List<string>? platforms,
        string configuration,
        string contentRoot,
        bool help)
    {
        Client = client;
        SkipBuild = skipBuild;
        WipeRelease = wipeRelease;
        HybridAcz = hybridAcz;
        Standalone = standalone;
        Platforms = platforms;
        Configuration = configuration;
        LogBuild = logBuild;
        ContentRoot = contentRoot;
        Help = help;
    }
}

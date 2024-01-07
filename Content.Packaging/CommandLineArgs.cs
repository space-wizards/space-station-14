using System.Diagnostics.CodeAnalysis;

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

    // CommandLineArgs, 3rd of her name.
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsed)
    {
        parsed = null;
        bool? client = null;
        var skipBuild = false;
        var wipeRelease = true;
        var hybridAcz = false;
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
            else if (arg == "--platform")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No platform provided");
                    return false;
                }

                platforms ??= new List<string>();
                platforms.Add(enumerator.Current);
            }
            else if (arg == "--help")
            {
                PrintHelp();
                return false;
            }
            else
            {
                Console.WriteLine("Unknown argument: {0}", arg);
            }
        }

        if (client == null)
        {
            Console.WriteLine("Client / server packaging unspecified.");
            return false;
        }

        parsed = new CommandLineArgs(client.Value, skipBuild, wipeRelease, hybridAcz, platforms);
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
  --platform            Platform for server builds. Default will output several x64 targets.
");
    }

    private CommandLineArgs(
        bool client,
        bool skipBuild,
        bool wipeRelease,
        bool hybridAcz,
        List<string>? platforms)
    {
        Client = client;
        SkipBuild = skipBuild;
        WipeRelease = wipeRelease;
        HybridAcz = hybridAcz;
        Platforms = platforms;
    }
}

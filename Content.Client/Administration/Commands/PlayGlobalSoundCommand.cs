using System.Linq;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Commands;

/// <summary>
/// Proxy to server-side <c>playglobalsound</c> command. Implements completions.
/// </summary>
public sealed class PlayGlobalSoundCommand : IConsoleCommand
{
    public string Command => "playglobalsound";
    public string Description => Loc.GetString("play-global-sound-command-description");
    public string Help => Loc.GetString("play-global-sound-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.RemoteExecuteCommand(argStr);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var hint = Loc.GetString("play-global-sound-command-arg-path");
            var res = IoCManager.Resolve<IResourceManager>();

            var curPath = args[0];
            if (curPath == "")
                curPath = "/";

            var resPath = new ResourcePath(curPath);

            if (!curPath.EndsWith("/"))
                resPath = (resPath / "..").Clean();

            var options = GetDirEntries(res, resPath)
                .OrderBy(c => c)
                .Select(c =>
                {
                    if (c.EndsWith("/"))
                        return new CompletionOption(c, Flags: CompletionOptionFlags.PartialCompletion);

                    return new CompletionOption(c);
                });

            return CompletionResult.FromHintOptions(options, hint);
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("play-global-sound-command-arg-volume"));

        if (args.Length > 2)
        {
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            var options = plyMgr.Sessions.Select(c => c.Name);
            return CompletionResult.FromHintOptions(
                options,
                Loc.GetString("play-global-sound-command-arg-usern", ("user", args.Length - 2)));
        }

        return CompletionResult.Empty;
    }

    private static IEnumerable<string> GetDirEntries(IResourceManager mgr, ResourcePath path)
    {
        var countDirs = path.EnumerateSegments().Count();

        var options = mgr.ContentFindFiles(path).Select(c =>
        {
            var segCount = c.EnumerateSegments().Count();
            var newPath = (path / c.EnumerateSegments().Skip(countDirs).First()).ToString();
            if (segCount > countDirs + 1)
                newPath += "/";

            return newPath;
        }).Distinct();

        return options;
    }
}

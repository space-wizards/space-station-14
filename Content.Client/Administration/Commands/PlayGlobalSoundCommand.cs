using System.Linq;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;

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

            var options = CompletionHelper.ContentFilePath(args[0], res);

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
}

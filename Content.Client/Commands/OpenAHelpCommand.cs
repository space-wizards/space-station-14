using Content.Client.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client.Commands
{
    [AnyCommand]
    public class OpenAHelpCommand : IConsoleCommand
    {
        public string Command => "openahelp";
        public string Description => $"Opens AHelp channel for a given NetUserID, or your personal channel if none given.";
        public string Help => $"{Command} [<username>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length >= 2)
            {
                shell.WriteLine(Help);
                return;
            }
            if (args.Length == 0)
            {
                EntitySystem.Get<BwoinkSystem>().EnsureWindowForLocalPlayer();
            }
            else
            {
                EntitySystem.Get<BwoinkSystem>().EnsureWindow(args[0]);
            }
        }
    }
}

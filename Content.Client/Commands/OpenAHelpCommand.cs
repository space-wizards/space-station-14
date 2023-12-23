using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client.Commands
{
    [AnyCommand]
    public sealed class OpenAHelpCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "openahelp";
        public string Description => Loc.GetString("open-a-help-command-description");
        public string Help => Loc.GetString("open-a-help-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length >= 2)
            {
                shell.WriteLine(Help);
                return;
            }
            if (args.Length == 0)
            {
                IoCManager.Resolve<IUserInterfaceManager>().GetUIController<AHelpUIController>().Open();
            }
            else
            {
                if (Guid.TryParse(args[0], out var guid))
                {
                    var targetUser = new NetUserId(guid);
                    IoCManager.Resolve<IUserInterfaceManager>().GetUIController<AHelpUIController>().Open(targetUser);
                }
                else
                {
                    shell.WriteError(Loc.GetString("open-a-help-command-error"));
                }
            }
        }
    }
}

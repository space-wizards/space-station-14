using System;
using Content.Client.Administration;
using Content.Client.Administration.Systems;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Client.Commands
{
    [AnyCommand]
    public sealed class OpenAHelpCommand : IConsoleCommand
    {
        public string Command => "openahelp";
        public string Description => $"Opens AHelp channel for a given NetUserID, or your personal channel if none given.";
        public string Help => $"{Command} [<netuserid>]";

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
                    shell.WriteLine("Bad GUID!");
                }
            }
        }
    }
}

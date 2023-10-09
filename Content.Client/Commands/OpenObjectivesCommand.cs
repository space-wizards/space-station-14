using Content.Client.UserInterface.Systems.Admin;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client.Commands
{
    [AnyCommand]
    public sealed class OpenObjectivesCommand : IConsoleCommand
    {
        public string Command => "openobjectives";
        public string Description => $"Opens a list of targets for a given NetUserID.";
        public string Help => $"{Command} [<netuserid>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length >= 2)
            {
                shell.WriteLine(Help);
                return;
            }
            if (Guid.TryParse(args[0], out var guid))
            {
                var targetUser = new NetUserId(guid);
                IoCManager.Resolve<IUserInterfaceManager>().GetUIController<ObjectivesUIController>().OpenWindow(targetUser);
            }
            else
            {
                shell.WriteLine("Bad GUID!");
            }
        }
    }
}

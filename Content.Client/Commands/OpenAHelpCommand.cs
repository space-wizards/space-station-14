using System;
 using Content.Client.Administration;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    public class OpenAHelpCommand : IConsoleCommand
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
                EntitySystem.Get<BwoinkSystem>().EnsureWindowForLocalPlayer();
            }
            else
            {
                if (Guid.TryParse(args[0], out var guid))
                {
                    EntitySystem.Get<BwoinkSystem>().EnsureWindow(new NetUserId(guid));
                }
                else
                {
                    shell.WriteLine("Bad GUID!");
                    return;
                }
            }
        }
    }
}

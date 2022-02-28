using System;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Audio;

namespace Content.Server.Announcements
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AnnounceCommand : IConsoleCommand
    {
        public string Command => "announce";
        public string Description => "Send an in-game announcement.";
        public string Help => $"{Command} <sender> <message> or {Command} <message> to send announcement as centcomm.";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var chat = IoCManager.Resolve<IChatManager>();

            if (args.Length == 0)
            {
                shell.WriteError("Not enough arguments! Need at least 1.");
                return;
            }

            if (args.Length == 1)
            {
                chat.DispatchStationAnnouncement(args[0]);
            }
            else
            {
                var message = string.Join(' ', new ArraySegment<string>(args, 1, args.Length-1));
                chat.DispatchStationAnnouncement(message, args[0]);
            }
            shell.WriteLine("Sent!");
        }
    }
}

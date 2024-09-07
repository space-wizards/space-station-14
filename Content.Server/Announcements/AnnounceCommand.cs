using System.Linq;
using Content.Server.Administration;
using Content.Server.Announcements.Systems;
using Content.Shared.Administration;
using Content.Shared.Announcements.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Announcements
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class AnnounceCommand : IConsoleCommand
    {
        public string Command => "announce";
        public string Description => "Send an in-game announcement.";
        public string Help => $"{Command} <sender> <message> <sound> <announcer>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var announcer = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AnnouncerSystem>();
            var proto = IoCManager.Resolve<IPrototypeManager>();

            switch (args.Length)
            {
                case 0:
                    shell.WriteError("Not enough arguments! Need at least 1.");
                    return;
                case 1:
                    announcer.SendAnnouncement(announcer.GetAnnouncementId("CommandReport"), Filter.Broadcast(),
                        args[0], "Central Command", Color.Gold);
                    break;
                case 2:
                    announcer.SendAnnouncement(announcer.GetAnnouncementId("CommandReport"), Filter.Broadcast(),
                        args[1], args[0], Color.Gold);
                    break;
                case 3:
                    announcer.SendAnnouncement(announcer.GetAnnouncementId(args[2]), Filter.Broadcast(), args[1],
                        args[0], Color.Gold);
                    break;
                case 4:
                    if (!proto.TryIndex(args[3], out AnnouncerPrototype? prototype))
                    {
                        shell.WriteError($"No announcer prototype with ID {args[3]} found!");
                        return;
                    }
                    announcer.SendAnnouncement(args[2], Filter.Broadcast(), args[1], args[0], Color.Gold, null,
                        prototype);
                    break;
            }

            shell.WriteLine("Sent!");
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 3:
                {
                    var list = new List<string>();

                    foreach (var prototype in IoCManager.Resolve<IPrototypeManager>()
                                .EnumeratePrototypes<AnnouncerPrototype>()
                                .SelectMany<AnnouncerPrototype, string>(p => p.Announcements.Select(a => a.ID)))
                    {
                        if (!list.Contains(prototype))
                            list.Add(prototype);
                    }

                    return CompletionResult.FromHintOptions(list, Loc.GetString("admin-announce-hint-sound"));
                }
                case 4:
                {
                    var list = new List<string>();

                    foreach (var prototype in IoCManager.Resolve<IPrototypeManager>()
                        .EnumeratePrototypes<AnnouncerPrototype>())
                    {
                        if (!list.Contains(prototype.ID))
                            list.Add(prototype.ID);
                    }

                    return CompletionResult.FromHintOptions(list, Loc.GetString("admin-announce-hint-voice"));
                }
                default:
                    return CompletionResult.Empty;
            }
        }
    }
}

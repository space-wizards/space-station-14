using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal class SayCommand : IConsoleCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteLine("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not {} playerEntity)
            {
                shell.WriteLine("You don't have an entity!");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var chatSanitizer = IoCManager.Resolve<IChatSanitizationManager>();

            if (IoCManager.Resolve<IEntityManager>().HasComponent<GhostComponent>(playerEntity))
                chat.SendDeadChat(player, message);
            else
            {
                var mindComponent = player.ContentData()?.Mind;

                if (mindComponent == null)
                {
                    shell.WriteError("You don't have a mind!");
                    return;
                }

                if (mindComponent.OwnedEntity is not {Valid: true} owned)
                {
                    shell.WriteError("You don't have an entity!");
                    return;
                }

                var emote = chatSanitizer.TrySanitizeOutSmilies(message, owned, out var sanitized, out var emoteStr);
                if (sanitized.Length != 0)
                    chat.EntitySay(owned, sanitized);
                if (emote)
                    chat.EntityMe(owned, emoteStr!);
            }

        }
    }
}

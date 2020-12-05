#nullable enable
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Chat
{
    [AnyCommand]
    internal class OOCCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out Of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendOOC(player, message);
        }
    }
}

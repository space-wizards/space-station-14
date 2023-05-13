using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.SS220.Discord.Commands;

[AnyCommand]
public sealed class DiscordCommand : IConsoleCommand
{
    /// <inheritdoc />
    public string Command => "discordlink";

    /// <inheritdoc />
    public string Description => Loc.GetString("discord-command-description");

    /// <inheritdoc />
    public string Help => Loc.GetString("discord-command-help");

    [Dependency] private readonly DiscordPlayerManager _discordPlayerManager = default!;

    /// <inheritdoc />
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession player)
            return;
        try
        {
            var key = await _discordPlayerManager.CheckAndGenerateKey(player.Data);
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(key))
            {
                sb.Append(Loc.GetString("discord-command-key-link", ("key", key)));
            }
            else
            {
                sb.Append(Loc.GetString("discord-command-already"));
            }

            var message = sb.ToString();
            IoCManager.Resolve<IChatManager>().ChatMessageToOne(ChatChannel.Server, message, message, default, false, player.ConnectedClient);
        }
        catch (Exception e)
        {
            shell.WriteLine("Произошла ошибка. Свяжитесь с администрацией");
        }
    }
}

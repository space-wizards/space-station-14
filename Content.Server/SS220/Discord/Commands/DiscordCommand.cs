// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;
using Robust.Shared.Player;

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
        if (shell.Player is not ICommonSession player)
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

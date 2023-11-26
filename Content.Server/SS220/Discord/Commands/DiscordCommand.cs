// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord.Commands;

[AnyCommand]
public sealed class DiscordCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly DiscordPlayerManager _discordPlayerManager = default!;

    public const string SawmillTitle = "discordLinkCommand";

    /// <inheritdoc />
    public string Command => "discordlink";

    /// <inheritdoc />
    public string Description => Loc.GetString("discord-command-description");

    /// <inheritdoc />
    public string Help => Loc.GetString("discord-command-help");

    /// <inheritdoc />
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            return;
        }

        try
        {
            var key = await _discordPlayerManager.CheckAndGenerateKey(player.Data);

            var linkEui = new DiscordLinkEui();
            _eui.OpenEui(linkEui, player);

            linkEui.SetLinkKey(key);
        }
        catch (Exception e)
        {
            _logManager.GetSawmill(SawmillTitle).Error("Error on discord link create {error}", e);

            shell.WriteLine("Произошла ошибка. Свяжитесь с администрацией");
        }
    }
}

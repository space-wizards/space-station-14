using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.CCVar;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

/// <summary>
/// A console command usable by any user which prints or sets the Message of the Day.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class SetMotdCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public string Command => "set-motd";
    public string Description => "Sets or clears the Message Of The Day.";
    public string Help => "motd [ <text> ... ]";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string motd = "";
        var player = (IPlayerSession?)shell.Player;
        if (args.Length > 0)
        {
            motd = string.Join(" ", args).Trim();
            if (player != null && _chatManager.MessageCharacterLimit(player, motd))
                return; // check function prints its own error response
        }
        
        _configurationManager.SetCVar(CCVars.MOTD, motd); // A hook in MOTDSystem broadcasts changes to the MOTD to everyone so we don't need to do it here.
        if (string.IsNullOrEmpty(motd))
        {
            shell.WriteLine(Loc.GetString("motd-command-cleared-message"));
            _adminLogManager.Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} cleared the MOTD for the server.");
        }
        else
        {
            shell.WriteLine(Loc.GetString("motd-command-set-message", ("motd", motd)));
            _adminLogManager.Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} set the MOTD for the server to \"{motd:motd}\"");
        }
    }
}

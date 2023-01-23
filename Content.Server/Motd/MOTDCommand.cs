using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Server.Commands;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

[AnyCommand]
internal sealed class MOTDCommand : IConsoleCommand
{
    #region Dependencies
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private MOTDSystem? _motdSystem = null; // Can't be a normal dependency because only entity systems can resolve entity systems as dependencies.
    #endregion Dependencies

    public string Command => "motd";
    public string Description => "Print or set the Message Of The Day (MOTD).";
    public string Help => "motd [ <text> ]";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = (IPlayerSession?) shell.Player;
        if (args.Length < 1)
        {   // Get MOTD
            _motdSystem ??= _entitySystemManager.GetEntitySystem<MOTDSystem>();
            _motdSystem.TrySendMOTD(shell);
            return;
        }

        // Set MOTD
        if (player != null && !_adminManager.HasAdminFlag(player, AdminFlags.Admin))
        {
            shell.WriteError($"You do not have the permissions necessary to set the message of the day for this server.");
            return;
        }

        var motd = string.Join(" ", args).Trim();
        if (player != null && _chatManager.MessageCharacterLimit(player, motd))
            return;
        
        _configurationManager.SetCVar(CCVars.MOTD, motd); // A hook in MOTDSystem broadcasts changes to the MOTD to everyone so we don't need to do it here.
        if (string.IsNullOrEmpty(motd))
            _adminLogManager.Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} cleared the MOTD for the server.");
        else
            _adminLogManager.Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} set the MOTD for the server to \"{motd:motd}\"");
    }
}

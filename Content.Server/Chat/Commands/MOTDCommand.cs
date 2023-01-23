using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class MOTDCommand : IConsoleCommand
{
    public string Command => "motd";
    public string Description => "Print or set the Message Of The Day (MOTD).";
    public string Help => "motd [ <text> ]";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = (IPlayerSession?) shell.Player;
        if (args.Length < 1)
        {   // Get MOTD
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>().TrySendMOTD(shell);
            return;
        }

        // Set MOTD
        var adMan = IoCManager.Resolve<IAdminManager>();
        if (player != null && !adMan.HasAdminFlag(player, AdminFlags.Admin))
            return;

        var motd = string.Join(" ", args).Trim();
        var chatMan = IoCManager.Resolve<IChatManager>();
        if (player != null && chatMan.MessageCharacterLimit(player, motd))
            return;
        
        IoCManager.Resolve<IConfigurationManager>().SetCVar(CCVars.MOTD, motd);
        if (string.IsNullOrEmpty(motd))
            IoCManager.Resolve<IAdminLogManager>().Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} cleared the MOTD for the server.");
        else
            IoCManager.Resolve<IAdminLogManager>().Add(LogType.Chat, LogImpact.Low, $"{(player == null ? "LOCALHOST" : player.ConnectedClient.UserName):Player} set the MOTD for the server to \"{motd:motd}\"");
    }
}

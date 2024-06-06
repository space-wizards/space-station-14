using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Info;

[AdminCommand(AdminFlags.Admin)]
public sealed class ShowRulesCommand : IConsoleCommand
{
    public string Command => "showrules";
    public string Description => "Opens the rules popup for the specified player.";
    public string Help => "showrules <username> [seconds]";
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        float seconds;

        switch (args.Length)
        {
            case 1:
            {
                target = args[0];
                var configurationManager = IoCManager.Resolve<IConfigurationManager>();
                seconds = configurationManager.GetCVar(CCVars.RulesWaitTime);
                break;
            }
            case 2:
            {
                if (!float.TryParse(args[1], out seconds))
                {
                    shell.WriteError($"{args[1]} is not a valid amount of seconds.\n{Help}");
                    return;
                }

                target = args[0];
                break;
            }
            default:
            {
                shell.WriteLine(Help);
                return;
            }
        }


        var message = new ShowRulesPopupMessage { PopupTime = seconds };

        if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(target, out var player))
        {
            shell.WriteError("Unable to find a player with that name.");
           return;
        }

        var netManager = IoCManager.Resolve<INetManager>();
        netManager.ServerSendMessage(message, player.Channel);
    }
}

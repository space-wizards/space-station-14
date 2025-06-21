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
public sealed class ShowRulesCommand : LocalizedCommands
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override string Command => "showrules";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        float seconds;

        if (!_player.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString($"shell-target-player-does-not-exist"));
            return;
        }

        switch (args.Length)
        {
            case 1:
            {
                seconds = _configuration.GetCVar(CCVars.RulesWaitTime);
                break;
            }
            case 2:
            {
                if (!float.TryParse(args[1], out seconds))
                {
                    shell.WriteError($"{args[1]} is not a valid amount of seconds.\n{Help}");
                    return;
                }

                break;
            }
            default:
            {
                shell.WriteLine(Help);
                return;
            }
        }

        var coreRules = _configuration.GetCVar(CCVars.RulesFile);
        var message = new SendRulesInformationMessage { PopupTime = seconds, CoreRules = coreRules, ShouldShowRules = true};
        _net.ServerSendMessage(message, player.Channel);
    }
}

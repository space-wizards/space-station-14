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
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override string Command => "showrules";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var seconds = _configuration.GetCVar(CCVars.RulesWaitTime);

        if (args.Length == 2 && !float.TryParse(args[1], out seconds))
        {
            shell.WriteError(Loc.GetString("cmd-showrules-invalid-seconds", ("seconds", args[1])));
            return;
        }

        if (!_player.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        var coreRules = _configuration.GetCVar(CCVars.RulesFile);
        var message = new SendRulesInformationMessage
            { PopupTime = seconds, CoreRules = coreRules, ShouldShowRules = true };
        _net.ServerSendMessage(message, player.Channel);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromOptions(CompletionHelper.SessionNames(players: _player))
            : CompletionResult.Empty;
    }
}

using System.Linq;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetDisplayNameCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "setdisplayname";
    public override string Description => Loc.GetString("cmd-displayname-set-description");
    public override string Help =>  Loc.GetString("cmd-displayname-set-help");

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(names, Loc.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (args.Length < 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(args[1], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (args.Length == 2)
        {
            var didFind = _playerManager.TryGetSessionByUsername(args[1], out player);
            if (!didFind)
            {
                shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        if (_playerManager.Sessions.Select(c => c.DisplayName).Contains(args[0]))
        {
            shell.WriteError(Loc.GetString("cmd-displayname-name-exists"));
        }

        _playerManager.SetDisplayName(player!, Loc.GetString(args[0]));
    }
}

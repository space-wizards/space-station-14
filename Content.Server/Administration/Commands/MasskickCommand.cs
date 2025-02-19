using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.MassKick)]
public sealed class MasskickCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    public override string Command => "masskick";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("cmd-masskick-help"));

            return;
        }

        var reason = $"Kicked by console: {args[0]}";

        for (var i = 1; i < args.Length; i++)
        {
            if (!_players.TryGetSessionByUsername(args[i], out var target))
            {
                shell.WriteError(Loc.GetString("cmd-masskick-invalid-player-skipped", ("player", args[i])));

                continue;
            }

            _netManager.DisconnectChannel(target.Channel, reason);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("[<Reason>]");

        if (args.Length > 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, "<PlayerIndex>");
        }

        return CompletionResult.Empty;
    }
}

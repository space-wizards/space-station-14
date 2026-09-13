using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Afk;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class CheckAfkCommand : LocalizedEntityCommands
{
    [Dependency] private AfkConfirmSystem _afkConfirm = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override string Command => "checkafk";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-checkafk-invalid-arguments"));
            return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (located is null)
        {
            shell.WriteError(Loc.GetString("cmd-checkafk-invalid-player"));
            return;
        }

        if (!_players.TryGetSessionById(located.UserId, out var session)
            || session.Status == SessionStatus.Disconnected)
        {
            shell.WriteError(Loc.GetString("cmd-checkafk-not-attached"));
            return;
        }

        if (!_afkConfirm.TryStartConfirmation(session))
        {
            shell.WriteError(Loc.GetString("cmd-checkafk-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-checkafk-sent", ("player", session.Name)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name);
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-checkafk-hint"));
    }
}

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Connection;

[AdminCommand(AdminFlags.Moderator)]
public sealed class GrantConnectBypassCommand : LocalizedCommands
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;

    public override string Command => "grant_connect_bypass";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-invalid-args"));
            return;
        }

        var argPlayer = args[0];
        var info = await _playerLocator.LookupIdByNameOrIdAsync(argPlayer);
        if (info == null)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-unknown-user", ("user", argPlayer)));
            return;
        }

        var duration = DefaultDuration;
        if (args.Length > 1)
        {
            var argDuration = args[2];
            if (!uint.TryParse(argDuration, out var minutes))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-invalid-duration", ("duration", argDuration)));
                return;
            }

            duration = TimeSpan.FromMinutes(minutes);
        }

        _connectionManager.AddTemporaryConnectBypass(info.UserId, duration);
        shell.WriteLine(Loc.GetString($"cmd-{Command}-success", ("user", argPlayer)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-user"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-duration"));

        return CompletionResult.Empty;
    }
}

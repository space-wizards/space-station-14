using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Connection.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddBlacklistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public override string Command => "blacklistadd";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var data = await _playerLocator.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-not-found", ("username", args[0])));
            return;
        }
        var guid = data.UserId;
        var isBlacklisted = await _db.GetBlacklistStatusAsync(guid);
        if (isBlacklisted)
        {
            shell.WriteLine(Loc.GetString($"cmd-{Command}-existing", ("username", data.Username)));
            return;
        }

        await _db.AddToBlacklistAsync(guid);
        shell.WriteLine(Loc.GetString($"cmd-{Command}-added", ("username", data.Username)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveBlacklistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public override string Command => "blacklistremove";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var data = await _playerLocator.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-not-found", ("username", args[0])));
            return;
        }

        var guid = data.UserId;
        var isBlacklisted = await _db.GetBlacklistStatusAsync(guid);
        if (!isBlacklisted)
        {
            shell.WriteLine(Loc.GetString($"cmd-{Command}-existing", ("username", data.Username)));
            return;
        }

        await _db.RemoveFromBlacklistAsync(guid);
        shell.WriteLine(Loc.GetString($"cmd-{Command}-removed", ("username", data.Username)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

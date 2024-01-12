using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Connection;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddBlacklistCommand : LocalizedCommands
{
    public override string Command => "blacklistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = string.Join(' ', args).Trim();
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isBlacklisted = await db.GetBlacklistStatusAsync(guid);
            if (isBlacklisted)
            {
                shell.WriteLine(Loc.GetString("cmd-blacklistadd-existing", ("username", data.Username)));
                return;
            }

            await db.AddToBlacklistAsync(guid);
            shell.WriteLine(Loc.GetString("cmd-blacklistadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-blacklistadd-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-blacklistadd-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveBlacklistCommand : LocalizedCommands
{
    public override string Command => "blacklistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = string.Join(' ', args).Trim();
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isBlacklisted = await db.GetBlacklistStatusAsync(guid);
            if (!isBlacklisted)
            {
                shell.WriteLine(Loc.GetString("cmd-blacklistremove-existing", ("username", data.Username)));
                return;
            }

            await db.RemoveFromBlacklistAsync(guid);
            shell.WriteLine(Loc.GetString("cmd-blacklistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-blacklistremove-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-blacklistremove-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

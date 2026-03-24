using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameBanCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernameban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernameban-help"));
            return;
        }

        var username = args[0];
        var note = args.Length > 1 ? string.Join(" ", args[1..]) : null;

        var admin = shell.Player?.UserId;

        try
        {
            var id = await _db.AddUsernameExactBanAsync(username, note, null, admin);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernameban-success", ("username", username), ("id", id)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameban-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameUnbanCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernameunban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernameunban-help"));
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            shell.WriteError(Loc.GetString("cmd-usernameunban-invalid-id"));
            return;
        }

        var admin = shell.Player?.UserId;
        if (admin == null)
        {
            shell.WriteError(Loc.GetString("cmd-usernameunban-no-admin"));
            return;
        }

        try
        {
            await _db.DeleteUsernameExactBanAsync(id, admin.Value);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernameunban-success", ("id", id)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameunban-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernamewhitelist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernamewhitelist-help"));
            return;
        }

        var username = args[0];
        var note = args.Length > 1 ? string.Join(" ", args[1..]) : null;

        var admin = shell.Player?.UserId;

        try
        {
            var id = await _db.AddUsernameWhitelistAsync(username, note, admin);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernamewhitelist-success", ("username", username), ("id", id)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernamewhitelist-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameUnwhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernameunwhitelist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernameunwhitelist-help"));
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            shell.WriteError(Loc.GetString("cmd-usernameunwhitelist-invalid-id"));
            return;
        }

        try
        {
            await _db.DeleteUsernameWhitelistAsync(id);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernameunwhitelist-success", ("id", id)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameunwhitelist-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameRegexBanCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernameregexban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexban-help"));
            return;
        }

        var pattern = args[0];
        var note = args.Length > 1 ? string.Join(" ", args[1..]) : null;

        var admin = shell.Player?.UserId;

        try
        {
            // Test the regex to make sure it's valid
            _ = new System.Text.RegularExpressions.Regex(pattern);

            var id = await _db.AddUsernameRegexBanAsync(pattern, note, null, false, admin);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernameregexban-success", ("pattern", pattern), ("id", id)));
        }
        catch (ArgumentException ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexban-invalid-pattern", ("error", ex.Message)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexban-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameRegexUnbanCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernameregexunban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexunban-help"));
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexunban-invalid-id"));
            return;
        }

        var admin = shell.Player?.UserId;
        if (admin == null)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexunban-no-admin"));
            return;
        }

        try
        {
            await _db.DeleteUsernameRegexBanAsync(id, admin.Value);
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernameregexunban-success", ("id", id)));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernameregexunban-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class UsernameRefreshCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameBanManager _usernameBanManager = default!;

    public override string Command => "usernamerefresh";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            await _usernameBanManager.RefreshCacheAsync();
            shell.WriteLine(Loc.GetString("cmd-usernamerefresh-success"));
        }
        catch (Exception ex)
        {
            shell.WriteError(Loc.GetString("cmd-usernamerefresh-error", ("error", ex.Message)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

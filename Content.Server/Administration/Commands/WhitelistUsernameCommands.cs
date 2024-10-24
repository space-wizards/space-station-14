using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class WhitelistAddUsernameCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;

    public override string Command => "wladdusername";
    public override string Help => Loc.GetString("cmd-whitelist-username-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-need-exactly-one-argument"));
            return;
        }

        string username = args[0];

        if (!UsernameHelpers.IsNameValid(username, out var reason))
        {
            shell.WriteError(Loc.GetString("cmd-ban-username-invalid-username", ("reason", reason)));
            return;
        }

        await _usernameRules.WhitelistAddUsernameAsync(username);
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class WhitelistRemoveUsernameCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;

    public override string Command => "wlrmusername";

    public override string Help => Loc.GetString("cmd-whitelist-username-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-need-exactly-one-argument"));
            return;
        }

        string username = args[0];

        var wasPresent = await _usernameRules.WhitelistRemoveUsernameAsync(username);
        if (!wasPresent)
        {
            shell.WriteError(Loc.GetString("cmd-username-whitelist-not-in-db"));
        }
    }
}

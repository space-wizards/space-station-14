using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

/*
 * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
 */

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class BabyJailCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "babyjail";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = Toggle(CCVars.BabyJailEnabled, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value ? "babyjail-command-enabled" : "babyjail-command-disabled"));
    }

    public static bool? Toggle(CVarDef<bool> cvar, IConsoleShell shell, string[] args, IConfigurationManager config)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
            return null;
        }

        var enabled = config.GetCVar(cvar);

        switch (args.Length)
        {
            case 0:
                enabled = !enabled;
                break;
            case 1 when !bool.TryParse(args[0], out enabled):
                shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
                return null;
        }

        config.SetCVar(cvar, enabled);

        return enabled;
    }
}


[AdminCommand(AdminFlags.Server)]
public sealed class BabyJailShowReasonCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "babyjail_show_reason";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = BabyJailCommand.Toggle(CCVars.BabyJailShowReason, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value
            ? "babyjail-command-show-reason-enabled"
            : "babyjail-command-show-reason-disabled"
        ));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class BabyJailMinAccountAgeCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "babyjail_max_account_age";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
            {
                var current = _cfg.GetCVar(CCVars.BabyJailMaxAccountAge);
                shell.WriteLine(Loc.GetString("babyjail-command-max-account-age-is", ("minutes", current)));
                break;
            }
            case > 1:
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
                return;
        }

        if (!int.TryParse(args[0], out var minutes))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        _cfg.SetCVar(CCVars.BabyJailMaxAccountAge, minutes);
        shell.WriteLine(Loc.GetString("babyjail-command-max-account-age-set", ("minutes", minutes)));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class BabyJailMinOverallHoursCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "babyjail_max_overall_minutes";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        switch (args.Length)
        {
            case 0:
            {
                var current = _cfg.GetCVar(CCVars.BabyJailMaxOverallMinutes);
                shell.WriteLine(Loc.GetString("babyjail-command-max-overall-minutes-is", ("minutes", current)));
                break;
            }
            case > 1:
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
                return;
        }

        if (!int.TryParse(args[0], out var hours))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        _cfg.SetCVar(CCVars.BabyJailMaxOverallMinutes, hours);
        shell.WriteLine(Loc.GetString("babyjail-command-overall-minutes-set", ("hours", hours)));
    }
}

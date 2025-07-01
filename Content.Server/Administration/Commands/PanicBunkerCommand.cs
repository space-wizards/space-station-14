using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = Toggle(CCVars.PanicBunkerEnabled, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value ? "panicbunker-command-enabled" : "panicbunker-command-disabled"));
    }

    public static bool? Toggle(CVarDef<bool> cvar, IConsoleShell shell, string[] args, IConfigurationManager config)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
            return null;
        }

        var enabled = config.GetCVar(cvar);

        if (args.Length == 0)
        {
            enabled = !enabled;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out enabled))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return null;
        }

        config.SetCVar(cvar, enabled);
        return enabled;
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerDisableWithAdminsCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_disable_with_admins";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = PanicBunkerCommand.Toggle(CCVars.PanicBunkerDisableWithAdmins, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value
            ? "panicbunker-command-disable-with-admins-enabled"
            : "panicbunker-command-disable-with-admins-disabled"
        ));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerEnableWithoutAdminsCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_enable_without_admins";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = PanicBunkerCommand.Toggle(CCVars.PanicBunkerEnableWithoutAdmins, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value
            ? "panicbunker-command-enable-without-admins-enabled"
            : "panicbunker-command-enable-without-admins-disabled"
        ));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerCountDeadminnedCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_count_deadminned_admins";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = PanicBunkerCommand.Toggle(CCVars.PanicBunkerCountDeadminnedAdmins, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value
            ? "panicbunker-command-count-deadminned-admins-enabled"
            : "panicbunker-command-count-deadminned-admins-disabled"
        ));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerShowReasonCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_show_reason";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var toggle = PanicBunkerCommand.Toggle(CCVars.PanicBunkerShowReason, shell, args, _cfg);
        if (toggle == null)
            return;

        shell.WriteLine(Loc.GetString(toggle.Value
            ? "panicbunker-command-show-reason-enabled"
            : "panicbunker-command-show-reason-disabled"
        ));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerMinAccountAgeCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_min_account_age";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            var current = _cfg.GetCVar(CCVars.PanicBunkerMinAccountAge);
            shell.WriteLine(Loc.GetString("panicbunker-command-min-account-age-is", ("minutes", current)));
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
            return;
        }

        if (!int.TryParse(args[0], out var minutes))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        _cfg.SetCVar(CCVars.PanicBunkerMinAccountAge, minutes);
        shell.WriteLine(Loc.GetString("panicbunker-command-min-account-age-set", ("minutes", minutes)));
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerMinOverallMinutesCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "panicbunker_min_overall_minutes";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            var current = _cfg.GetCVar(CCVars.PanicBunkerMinOverallMinutes);
            shell.WriteLine(Loc.GetString("panicbunker-command-min-overall-minutes-is", ("minutes", current)));
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
            return;
        }

        if (!int.TryParse(args[0], out var minutes))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        _cfg.SetCVar(CCVars.PanicBunkerMinOverallMinutes, minutes);
        shell.WriteLine(Loc.GetString("panicbunker-command-min-overall-minutes-set", ("minutes", minutes)));
    }
}

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Holiday;
using Robust.Shared.Console;

namespace Content.Server.Holiday;

/// <summary>
///     Admin command for setting the date for holidays.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class HolidayRefreshCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedHolidaySystem _holidaySys = default!;

    public override string Command => "holidayrefresh";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        DateTime date;

        switch (args.Length)
        {
            case 0:
                // Set holidays to today.
                _holidaySys.RefreshCurrentHolidays();
                shell.WriteLine(Loc.GetString("shell-command-success"));
                return;

            case 2:
                if (!int.TryParse(args[0], out var day2) ||
                    !int.TryParse(args[1], out var month2))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                    return;
                }
                date = new DateTime(DateTime.Now.Year, month2, day2);
                break;

            case 3:
                if (!int.TryParse(args[0], out var day3) ||
                    !int.TryParse(args[1], out var month3) ||
                    !int.TryParse(args[2], out var year3))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                    return;
                }
                date = new DateTime(year3, month3, day3);
                break;

            default:
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 2), ("upper", 3)));
                return;
        }

        _holidaySys.RefreshCurrentHolidays(date);
        shell.WriteLine(Loc.GetString("shell-command-success"));
    }
}

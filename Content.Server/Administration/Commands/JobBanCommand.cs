using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class JobBanCommand : IConsoleCommand
{
    public string Command => "jobban";
    public string Description => "Bans a player from a job";
    public string Help => $"Usage: {Command} <name or user ID> <job> <reason> [duration in minutes, leave out or 0 for permanent ban]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        string job;
        string reason;
        uint minutes;

        switch (args.Length)
        {
            case 3:
                target = args[0];
                job = args[1];
                reason = args[2];
                minutes = 0;
                break;
            case 4:
                target = args[0];
                job = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteLine($"{args[3]} is not a valid amount of minutes.\n{Help}");
                    return;
                }

                break;
            default:
                shell.WriteLine($"Invalid amount of arguments.");
                shell.WriteLine(Help);
                return;
        }

        IoCManager.Resolve<RoleBanManager>().CreateJobBan(shell, target, job, reason, minutes);
    }
}

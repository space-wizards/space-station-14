using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Commands;

#if DEBUG
[AdminCommand(AdminFlags.Host)]
public sealed class AdminLogBulk : IConsoleCommand
{
    public string Command => "adminlogbulk";
    public string Description => "Adds debug logs to the database.";
    public string Help => $"Usage: {Command} <amount> <parallel>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } entity)
        {
            shell.WriteError("This command can only be ran by a player with an attached entity.");
            return;
        }

        int amount;
        var parallel = false;

        switch (args)
        {
            case {Length: 1} when int.TryParse(args[0], out amount):
            case {Length: 2} when int.TryParse(args[0], out amount) &&
                                  bool.TryParse(args[1], out parallel):
                break;
            default:
                shell.WriteError(Help);
                return;
        }

        var logManager = IoCManager.Resolve<IAdminLogManager>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        if (parallel)
        {
            Parallel.For(0, amount, _ =>
            {
                logManager.Add(LogType.Unknown, $"Debug log added by {entity:Player}");
            });
        }
        else
        {
            for (var i = 0; i < amount; i++)
            {
                logManager.Add(LogType.Unknown, $"Debug log added by {entity:Player}");
            }
        }

        shell.WriteLine($"Added {amount} logs in {stopwatch.Elapsed.TotalMilliseconds} ms");
    }
}
#endif

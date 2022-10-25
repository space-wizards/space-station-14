using Content.Shared.Administration;
using JetBrains.Profiler.Api;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Administration.Commands;

#if !FULL_RELEASE
[AdminCommand(AdminFlags.Host)]
public sealed class ProfileEntitySpawningCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "profileEntitySpawning";
    public string Description => "Profiles entity spawning with n entities";
    public string Help => $"Usage: {Command} | {Command} <amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var amount = 10000;
        switch (args.Length)
        {
            case 0:
                break;
            case 1:
                if (!int.TryParse(args[0], out amount))
                {
                    shell.WriteError($"First argument is not an integer: {args[0]}");
                    return;
                }

                break;
            default:
                shell.WriteError(Help);
                return;
        }

        MeasureProfiler.StartCollectingData();

        for (var i = 0; i < amount; i++)
        {
            _entities.SpawnEntity(null, MapCoordinates.Nullspace);
        }

        MeasureProfiler.SaveData($"Server: Spawning {amount} entities");
        shell.WriteLine($"Server: Profiled spawning {amount} entities");
    }
}
#endif

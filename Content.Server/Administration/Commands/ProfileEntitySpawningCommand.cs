#if !FULL_RELEASE
using Content.Shared.Administration;
using JetBrains.Profiler.Api;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class ProfileEntitySpawningCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "profileEntitySpawning";
    public string Description => "Profiles entity spawning with n entities";
    public string Help => $"Usage: {Command} | {Command} <amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var amount = 1000;
        string? prototype = null;

        switch (args.Length)
        {
            case 0:
                break;
            case 2:
                if (!int.TryParse(args[0], out amount))
                {
                    shell.WriteError($"First argument is not an integer: {args[0]}");
                    return;
                }

                prototype = args[1];

                break;
            default:
                shell.WriteError(Help);
                return;
        }

        MeasureProfiler.StartCollectingData();

        for (var i = 0; i < amount; i++)
        {
            _entities.SpawnEntity(prototype, MapCoordinates.Nullspace);
        }

        MeasureProfiler.SaveData();
        shell.WriteLine($"Server: Profiled spawning {amount} entities");
    }
}
#endif

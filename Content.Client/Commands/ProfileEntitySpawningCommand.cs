using JetBrains.Profiler.Api;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Client.Commands;

#if !FULL_RELEASE
public sealed class ProfileEntitySpawningCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "profileEntitySpawning";
    public string Description => "Profiles entity spawning with n entities";
    public string Help => $"Usage: {Command} | {Command} <amount> <prototype>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var amount = 10000;
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

        MeasureProfiler.SaveData($"Client: Spawning {amount} entities");
        shell.WriteLine($"Client: Profiled spawning {amount} entities");
    }
}
#endif

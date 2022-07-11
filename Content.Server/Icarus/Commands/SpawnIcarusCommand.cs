using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Icarus.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.Fun)]
public sealed class SpawnIcarusCommand : IConsoleCommand
{
    public string Command => "spawnicarus";
    public string Description => "Spawn Icarus beam and direct to specified grid center.";
    public string Help => "spawnicarus <gridId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Incorrect number of arguments. " + Help);
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            shell.WriteLine($"{args[0]} is not a valid integer.");
            return;
        }

        var gridId = new GridId(int.Parse(args[0]));
        var mapManager = IoCManager.Resolve<IMapManager>();

        if (mapManager.TryGetGrid(gridId, out var grid))
        {
            var icarusSystem = EntitySystem.Get<IcarusTerminalSystem>();
            var coords = icarusSystem.FireBeam(grid.WorldAABB);
            shell.WriteLine($"Icarus was spawned: {coords.ToString()}");
        }
        else
        {
            shell.WriteError($"No grid exists with id {id}");
        }
    }
}

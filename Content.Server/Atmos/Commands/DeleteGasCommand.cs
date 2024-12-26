using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class DeleteGasCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "deletegas";
        public string Description => "Removes all gases from a grid, or just of one type if specified.";
        public string Help => $"Usage: {Command} <GridId> <Gas> / {Command} <GridId> / {Command} <Gas> / {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            EntityUid? gridId;
            Gas? gas = null;

            switch (args.Length)
            {
                case 0:
                {
                    if (player == null)
                    {
                        shell.WriteLine("A grid must be specified when the command isn't used by a player.");
                        return;
                    }

                    if (player.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine("You have no entity to get a grid from.");
                        return;
                    }

                    gridId = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                    if (gridId == null)
                    {
                        shell.WriteLine("You aren't on a grid to delete gas from.");
                        return;
                    }

                    break;
                }
                case 1:
                {
                    if (!NetEntity.TryParse(args[0], out var numberEnt) || !_entManager.TryGetEntity(numberEnt, out var number))
                    {
                        // Argument is a gas
                        if (player == null)
                        {
                            shell.WriteLine("A grid id must be specified if not using this command as a player.");
                            return;
                        }

                        if (player.AttachedEntity is not {Valid: true} playerEntity)
                        {
                            shell.WriteLine("You have no entity from which to get a grid id.");
                            return;
                        }

                        gridId = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                        if (gridId == null)
                        {
                            shell.WriteLine("You aren't on a grid to delete gas from.");
                            return;
                        }

                        if (!Enum.TryParse<Gas>(args[0], true, out var parsedGas))
                        {
                            shell.WriteLine($"{args[0]} is not a valid gas name.");
                            return;
                        }

                        gas = parsedGas;
                        break;
                    }

                    // Argument is a grid
                    gridId = number;
                    break;
                }
                case 2:
                {
                    if (!NetEntity.TryParse(args[0], out var firstNet) || !_entManager.TryGetEntity(firstNet, out var first))
                    {
                        shell.WriteLine($"{args[0]} is not a valid integer for a grid id.");
                        return;
                    }

                    gridId = first;

                    if (gridId.Value.IsValid())
                    {
                        shell.WriteLine($"{gridId} is not a valid grid id.");
                        return;
                    }

                    if (!Enum.TryParse<Gas>(args[1], true, out var parsedGas))
                    {
                        shell.WriteLine($"{args[1]} is not a valid gas.");
                        return;
                    }

                    gas = parsedGas;

                    break;
                }
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!_entManager.TryGetComponent<MapGridComponent>(gridId, out _))
            {
                shell.WriteLine($"No grid exists with id {gridId}");
                return;
            }

            var atmosphereSystem = _entManager.System<AtmosphereSystem>();

            var tiles = 0;
            var moles = 0f;

            if (gas == null)
            {
                foreach (var tile in atmosphereSystem.GetAllMixtures(gridId.Value, true))
                {
                    if (tile.Immutable)
                        continue;

                    tiles++;
                    moles += tile.TotalMoles;

                    tile.Clear();
                }
            }
            else
            {
                foreach (var tile in atmosphereSystem.GetAllMixtures(gridId.Value, true))
                {
                    if (tile.Immutable)
                        continue;

                    tiles++;
                    moles += tile.TotalMoles;

                    tile.SetMoles(gas.Value, 0);
                }
            }

            if (gas == null)
            {
                shell.WriteLine($"Removed {moles} moles from {tiles} tiles.");
                return;
            }

            shell.WriteLine($"Removed {moles} moles of gas {gas} from {tiles} tiles.");
        }
    }

}

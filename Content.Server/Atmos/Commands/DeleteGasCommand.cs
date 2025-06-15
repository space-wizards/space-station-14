using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class DeleteGasCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        public override string Command => "deletegas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-must-specify-grid"));
                        return;
                    }

                    if (player.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-no-entity"));
                        return;
                    }

                    gridId = EntityManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                    if (gridId == null)
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-not-on-grid"));
                        return;
                    }

                    break;
                }
                case 1:
                {
                    if (!NetEntity.TryParse(args[0], out var numberEnt) || !EntityManager.TryGetEntity(numberEnt, out var number))
                    {
                        // Argument is a gas
                        if (player == null)
                        {
                            shell.WriteLine(Loc.GetString($"cmd-deletegas-must-specify-grid"));
                            return;
                        }

                        if (player.AttachedEntity is not {Valid: true} playerEntity)
                        {
                            shell.WriteLine(Loc.GetString($"cmd-deletegas-no-entity"));
                            return;
                        }

                        gridId = EntityManager.GetComponent<TransformComponent>(playerEntity).GridUid;

                        if (gridId == null)
                        {
                            shell.WriteLine(Loc.GetString($"cmd-deletegas-not-on-grid"));
                            return;
                        }

                        if (!Enum.TryParse<Gas>(args[0], true, out var parsedGas))
                        {
                            shell.WriteLine(Loc.GetString($"cmd-deletegas-invalid-gas", ("gas", args[0])));
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
                    if (!NetEntity.TryParse(args[0], out var firstNet) || !EntityManager.TryGetEntity(firstNet, out var first))
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-invalid-grid-id", ("id", args[0])));
                        return;
                    }

                    gridId = first;

                    if (gridId.Value.IsValid())
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-invalid-grid-id", ("id", gridId)));
                        return;
                    }

                    if (!Enum.TryParse<Gas>(args[1], true, out var parsedGas))
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletegas-invalid-gas", ("gas", args[1])));
                        return;
                    }

                    gas = parsedGas;

                    break;
                }
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!EntityManager.TryGetComponent<MapGridComponent>(gridId, out _))
            {
                shell.WriteLine(Loc.GetString($"cmd-deletegas-invalid-grid-id", ("id", gridId)));
                return;
            }

            var tiles = 0;
            var moles = 0f;

            if (gas == null)
            {
                foreach (var tile in _atmosSystem.GetAllMixtures(gridId.Value, true))
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
                foreach (var tile in _atmosSystem.GetAllMixtures(gridId.Value, true))
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
                ;
                shell.WriteLine(Loc.GetString($"cmd-deletegas-success-no-gas", ("moles", moles), ("tiles", tiles)));
                return;
            }

            shell.WriteLine(Loc.GetString($"cmd-deletegas-success-gas", ("moles", moles), ("gas", gas), ("tiles", tiles)));
        }
    }

}

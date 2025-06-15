using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class FillGas : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        public override string Command => "fillgas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
                return;

            if (!NetEntity.TryParse(args[0], out var gridIdNet)
                || !EntityManager.TryGetEntity(gridIdNet, out var gridId)
                || !(AtmosCommandUtils.TryParseGasID(args[1], out var gasId))
                || !float.TryParse(args[2], out var moles))
            {
                return;
            }

            if (!EntityManager.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteError(Loc.GetString($"shell-invalid-grid-id-specific", ("grid", gridId)));
                return;
            }

            foreach (var tile in _atmosSystem.GetAllMixtures(gridId.Value, true))
            {
                tile.AdjustMoles(gasId, moles);
            }
        }
    }

}

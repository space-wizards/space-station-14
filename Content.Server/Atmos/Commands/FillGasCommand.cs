using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class FillGas : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "fillgas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
                return;

            if (!NetEntity.TryParse(args[0], out var gridIdNet)
                || !_entManager.TryGetEntity(gridIdNet, out var gridId)
                || !(AtmosCommandUtils.TryParseGasID(args[1], out var gasId))
                || !float.TryParse(args[2], out var moles))
            {
                return;
            }

            if (!_entManager.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteLine(Loc.GetString("cmd-fillgas-invalid-grid", ("grid", gridId)));
                return;
            }

            var atmosphereSystem = _entManager.System<AtmosphereSystem>();

            foreach (var tile in atmosphereSystem.GetAllMixtures(gridId.Value, true))
            {
                tile.AdjustMoles(gasId, moles);
            }
        }
    }

}

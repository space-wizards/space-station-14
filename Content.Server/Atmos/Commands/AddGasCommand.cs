using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddGasCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        public override string Command => "addgas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5)
                return;

            if (!int.TryParse(args[0], out var x)
                || !int.TryParse(args[1], out var y)
                || !NetEntity.TryParse(args[2], out var netEnt)
                || !EntityManager.TryGetEntity(netEnt, out var euid)
                || !(AtmosCommandUtils.TryParseGasID(args[3], out var gasId))
                || !float.TryParse(args[4], out var moles))
            {
                return;
            }

            if (!EntityManager.HasComponent<MapGridComponent>(euid))
            {
                shell.WriteError(Loc.GetString($"shell-invalid-grid-id-specific", ("grid", euid)));
                return;
            }

            var indices = new Vector2i(x, y);
            var tile = _atmosSystem.GetTileMixture(euid, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-addgas-invalid-coordinates"));
                return;
            }

            tile.AdjustMoles(gasId, moles);
        }
    }
}

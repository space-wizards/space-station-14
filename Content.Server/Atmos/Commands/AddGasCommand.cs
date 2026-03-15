using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddGasCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "addgas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5)
                return;

            if (!int.TryParse(args[0], out var x)
                || !int.TryParse(args[1], out var y)
                || !NetEntity.TryParse(args[2], out var netEnt)
                || !_entManager.TryGetEntity(netEnt, out var euid)
                || !(AtmosCommandUtils.TryParseGasID(args[3], out var gasId))
                || !float.TryParse(args[4], out var moles))
            {
                return;
            }

            if (!_entManager.HasComponent<MapGridComponent>(euid))
            {
                shell.WriteError(Loc.GetString("cmd-addgas-not-grid", ("euid", euid)));
                return;
            }

            var atmosphereSystem = _entManager.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            var indices = new Vector2i(x, y);
            var tile = atmosphereSystem.GetTileMixture(euid, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine(Loc.GetString("cmd-addgas-invalid-coordinates-or-tile"));
                return;
            }

            tile.AdjustMoles(gasId, moles);
        }
    }
}

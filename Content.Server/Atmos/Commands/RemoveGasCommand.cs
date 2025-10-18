using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class RemoveGasCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override string Command => "removegas";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5)
                return;

            if (!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !NetEntity.TryParse(args[2], out var idNet)
               || !EntityManager.TryGetEntity(idNet, out var id)
               || !float.TryParse(args[3], out var amount)
               || !bool.TryParse(args[4], out var ratio))
            {
                return;
            }

            var indices = new Vector2i(x, y);
            var tile = _atmosphereSystem.GetTileMixture(id, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine(Loc.GetString("cmd-removegas-invalid-tile"));
                return;
            }

            if (ratio)
                tile.RemoveRatio(amount);
            else
                tile.Remove(amount);
        }
    }

}

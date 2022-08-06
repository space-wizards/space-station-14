using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class RemoveGasCommand : IConsoleCommand
    {
        public string Command => "removegas";
        public string Description => "Removes an amount of gases.";
        public string Help => "removegas <X> <Y> <GridId> <amount> <ratio>\nIf <ratio> is true, amount will be treated as the ratio of gas to be removed.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5) return;
            if(!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !EntityUid.TryParse(args[2], out var id)
               || !float.TryParse(args[3], out var amount)
               || !bool.TryParse(args[4], out var ratio)) return;

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var indices = new Vector2i(x, y);
            var tile = atmosphereSystem.GetTileMixture(id, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine("Invalid coordinates or tile.");
                return;
            }

            if (ratio)
                tile.RemoveRatio(amount);
            else
                tile.Remove(amount);
        }
    }

}

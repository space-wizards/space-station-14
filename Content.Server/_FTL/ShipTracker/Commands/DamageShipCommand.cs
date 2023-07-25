using Content.Server._FTL.Weapons;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


namespace Content.Server._FTL.ShipTracker.Commands;

public sealed class DamageShipCommand : LocalizedCommands
{
    [Dependency] private readonly EntityManager _ent = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override string Command => "damageship";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("You must have exactly 2 arguments.");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var gridUid))
        {
            shell.WriteError("Invalid specifier format.");
            return;
        }

        if (!_map.GridExists(gridUid))
        {
            shell.WriteError("Invalid grid.");
            return;
        }

        var outcome = _ent.System<ShipTrackerSystem>().TryDamageShip(gridUid, _proto.Index<FTLAmmoType>(args[1]), null);

        shell.WriteLine("Damaged ship. Hit: " + outcome.ToString());
    }
}

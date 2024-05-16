using Content.Server.Administration;
using Content.Server.Labels;
using Content.Shared.Administration;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;




namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Creates FTL disks, to maps, grids, or entities.
/// </summary>
[AdminCommand(AdminFlags.Fun)]

public sealed class FTLDiskBurnerCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

    public string Command => "FTLdiskburner";
    public string Description => Loc.GetString("ftl-disk-burner-desc");
    public string Help => Loc.GetString("cmd-ftl-disk-burner-help");

    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments.");
            return;
        }


        var player = shell.Player;

        if (player == null)
        {
            shell.WriteLine("Only a player can run this command.");
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteLine("You don't have an entity to spawn a disk at.");
            return;
        }

        EntityUid entity = player.AttachedEntity.Value;
        var coords = _entManager.GetComponent<TransformComponent>(entity).Coordinates;

        foreach (var destinations in args)
        {
            if (destinations == null)
                return;

            /// make sure destination is an id.
            EntityUid dest;
            if (_entManager.TryParseNetEntity(destinations, out var nullableDest))
            {
                if (nullableDest == null)
                    continue;

                dest = (EntityUid) nullableDest;

                /// we need to go to a map, so check if the EntID is something else then try for its map
                if (!_entManager.HasComponent<MapComponent>(dest))
                {
                    if (!_entManager.TryGetComponent<TransformComponent>(dest, out var entTransform))
                    {
                        shell.WriteLine(destinations + " has no Transform!");
                        continue;
                    }

                    var mapSystem = _entSystemManager.GetEntitySystem<SharedMapSystem>();
                    mapSystem.TryGetMap(entTransform.MapID, out var mapDest);
                    if (mapDest == null)
                    {
                        shell.WriteLine(destinations + " has no map to FTL to!");
                        continue;
                    }

                    dest = (EntityUid) mapDest;
                }

                /// check if our destination works already, if not, make it.
                if (!_entManager.HasComponent<FTLDestinationComponent>(dest))
                {
                    FTLDestinationComponent ftlDest = _entManager.AddComponent<FTLDestinationComponent>(dest);
                    ftlDest.RequireCoordinateDisk = true;
                }

                /// create the FTL disk
                EntityUid cdUid = _entManager.SpawnEntity(CoordinatesDisk, coords);
                var cd = _entManager.EnsureComponent<ShuttleDestinationCoordinatesComponent>(cdUid);
                cd.Destination = dest;
                _entManager.Dirty(cdUid, cd);

                if (_entManager.TryGetComponent<MetaDataComponent>(dest, out var meta) && meta != null && meta.EntityName != null)
                {
                    var labelSystem = _entSystemManager.GetEntitySystem<LabelSystem>();
                    labelSystem.Label(cdUid, meta.EntityName);
                }
            }
            else
            {
                shell.WriteLine(destinations + " is not an EntityID");
            }
        }
    }
}

using Content.Server.Administration;
using Content.Server.Labels;
using Content.Server.Shuttles.Components;
using Content.Shared.Administration;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;




namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Creates FTL disks, to maps, grids, or entities.
/// </summary>
[AdminCommand(AdminFlags.Fun)]

public sealed class FTLDiskBurnerCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

    public override string Command => "FTLdiskburner";

    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
            {
                shell.WriteLine("Destinations returned as null!");
                DebugTools.AssertNotNull(destinations);
                return;
            }

            // make sure destination is an id.
            EntityUid dest;
            if (_entManager.TryParseNetEntity(destinations, out var nullableDest))
            {
                if (nullableDest == null)
                    continue;

                dest = (EntityUid) nullableDest;

                // we need to go to a map, so check if the EntID is something else then try for its map
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

                // find and verify the map is not somehow unusable.
                if (!_entManager.TryGetComponent<MapComponent>(dest, out var mapComp))
                {
                    shell.WriteLine(destinations + " is somehow on map " + dest + " with no map component. What the fuck.");
                    continue;
                }
                if (mapComp.MapInitialized == false)
                {
                    shell.WriteLine(
                        destinations +
                        " is on map " +
                        dest +
                        " which is not initialized! Check it's safe to initialize, then initialize it first or the players will be stuck in place!"
                        );
                    continue;
                }
                if (mapComp.MapPaused == true)
                {
                    shell.WriteLine(
                        destinations +
                        " is on map " +
                        dest +
                        " which is paused! Are you certain you should be sending players here?"
                        );
                    continue;
                }

                // check if our destination works already, if not, make it.
                if (!_entManager.TryGetComponent<FTLDestinationComponent>(dest, out var ftlDestComp))
                {
                    FTLDestinationComponent ftlDest = _entManager.AddComponent<FTLDestinationComponent>(dest);
                    ftlDest.RequireCoordinateDisk = true;

                    if (_entManager.HasComponent<MapGridComponent>(dest))
                    {
                        ftlDest.BeaconsOnly = true;

                        shell.WriteLine(destinations + " is a planet map " + dest + " and will require an FTL point. It may already exist.");
                    }
                }
                else
                {
                    // we don't do these automatically, since it isn't clear what the correct resolution is. Instead we provide feedback to the user and carry on like they know what theyre doing.
                    if (ftlDestComp.Enabled == false)
                        shell.WriteLine(
                            destinations +
                            " is on map " +
                            dest +
                            " that already has an FTLDestinationComponent, but it is not Enabled! Set this manually for safety.");

                    if (ftlDestComp.BeaconsOnly == true)
                        shell.WriteLine(
                            destinations +
                            " is on map " +
                            dest +
                            " that requires a FTL point to travel to! It may already exist."
                            );
                }

                // create the FTL disk
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapUids(_entManager), "Map netId");
        return CompletionResult.Empty;
    }
}

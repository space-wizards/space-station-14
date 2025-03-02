using Content.Server.Administration;
using Content.Server.Labels;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Creates FTL disks, to maps, grids, or entities.
/// </summary>
[AdminCommand(AdminFlags.Fun)]

public sealed class FTLDiskCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntitySystemManager _entSystemManager = default!;

    public override string Command => "ftldisk";

    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";

    [ValidatePrototypeId<EntityPrototype>]
    public const string DiskCase = "DiskCase";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            return;
        }

        var player = shell.Player;

        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        EntityUid entity = player.AttachedEntity.Value;
        var coords = _entManager.GetComponent<TransformComponent>(entity).Coordinates;

        var handsSystem = _entSystemManager.GetEntitySystem<SharedHandsSystem>();
        var labelSystem = _entSystemManager.GetEntitySystem<LabelSystem>();
        var mapSystem = _entSystemManager.GetEntitySystem<SharedMapSystem>();
        var storageSystem = _entSystemManager.GetEntitySystem<SharedStorageSystem>();

        foreach (var destinations in args)
        {
            DebugTools.AssertNotNull(destinations);

            // make sure destination is an id.
            EntityUid dest;

            if (_entManager.TryParseNetEntity(destinations, out var nullableDest))
            {
                DebugTools.AssertNotNull(nullableDest);

                dest = (EntityUid) nullableDest;

                // we need to go to a map, so check if the EntID is something else then try for its map
                if (!_entManager.HasComponent<MapComponent>(dest))
                {
                    if (!_entManager.TryGetComponent<TransformComponent>(dest, out var entTransform))
                    {
                        shell.WriteLine(Loc.GetString("cmd-ftldisk-no-transform", ("destination", destinations)));
                        continue;
                    }

                    if (!mapSystem.TryGetMap(entTransform.MapID, out var mapDest))
                    {
                        shell.WriteLine(Loc.GetString("cmd-ftldisk-no-map", ("destination", destinations)));
                        continue;
                    }

                    DebugTools.AssertNotNull(mapDest);
                    dest = mapDest!.Value; // explicit cast here should be fine since the previous if should catch it.
                }

                // find and verify the map is not somehow unusable.
                if (!_entManager.TryGetComponent<MapComponent>(dest, out var mapComp)) // We have to check for a MapComponent here and above since we could have changed our dest entity.
                {
                    shell.WriteLine(Loc.GetString("cmd-ftldisk-no-map-comp", ("destination", destinations), ("map", dest)));
                    continue;
                }
                if (mapComp.MapInitialized == false)
                {
                    shell.WriteLine(Loc.GetString("cmd-ftldisk-map-not-init", ("destination", destinations), ("map", dest)));
                    continue;
                }
                if (mapComp.MapPaused == true)
                {
                    shell.WriteLine(Loc.GetString("cmd-ftldisk-map-paused", ("destination", destinations), ("map", dest)));
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

                        shell.WriteLine(Loc.GetString("cmd-ftldisk-planet", ("destination", destinations), ("map", dest)));
                    }
                }
                else
                {
                    // we don't do these automatically, since it isn't clear what the correct resolution is. Instead we provide feedback to the user and carry on like they know what theyre doing.
                    if (ftlDestComp.Enabled == false)
                        shell.WriteLine(Loc.GetString("cmd-ftldisk-already-dest-not-enabled", ("destination", destinations), ("map", dest)));

                    if (ftlDestComp.BeaconsOnly == true)
                        shell.WriteLine(Loc.GetString("cmd-ftldisk-requires-ftl-point", ("destination", destinations), ("map", dest)));
                }

                // create the FTL disk
                EntityUid cdUid = _entManager.SpawnEntity(CoordinatesDisk, coords);
                var cd = _entManager.EnsureComponent<ShuttleDestinationCoordinatesComponent>(cdUid);
                cd.Destination = dest;
                _entManager.Dirty(cdUid, cd);

                // create disk case
                EntityUid cdCaseUid = _entManager.SpawnEntity(DiskCase, coords);

                // apply labels
                if (_entManager.TryGetComponent<MetaDataComponent>(dest, out var meta) && meta != null && meta.EntityName != null)
                {
                    labelSystem.Label(cdUid, meta.EntityName);
                    labelSystem.Label(cdCaseUid, meta.EntityName);
                }

                // if the case has a storage, try to place the disk in there and then the case inhand

                if (_entManager.TryGetComponent<StorageComponent>(cdCaseUid, out var storage) && storageSystem.Insert(cdCaseUid, cdUid, out _, storageComp: storage, playSound: false))
                {
                    if (_entManager.TryGetComponent<HandsComponent>(entity, out var handsComponent) && handsSystem.TryGetEmptyHand(entity, out var emptyHand, handsComponent))
                    {
                        handsSystem.TryPickup(entity, cdCaseUid, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                    }
                }
                else // the case was messed up, put disk inhand
                {
                    _entManager.DeleteEntity(cdCaseUid); // something went wrong so just yeet the chaf

                    if (_entManager.TryGetComponent<HandsComponent>(entity, out var handsComponent) && handsSystem.TryGetEmptyHand(entity, out var emptyHand, handsComponent))
                    {
                        handsSystem.TryPickup(entity, cdUid, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                    }
                }
            }
            else
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-uid", ("uid", destinations)));
            }
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapUids(_entManager), Loc.GetString("cmd-ftldisk-hint"));
        return CompletionResult.Empty;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.Forensics;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Conditions;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.StationRecords;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Server.SS220.CryopodSSD;

/// <summary>
///             SS220
/// <seealso cref="SharedCryopodSSDSystem"/>
/// <seealso cref="CryopodSSDSystem"/>
/// <seealso cref="SSDStorageConsoleComponent"/>
/// </summary>
public sealed class SSDStorageConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly MindTrackerSystem _mindTrackerSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    
    private ISawmill _sawmill = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        _sawmill = Logger.GetSawmill("SSDStorageConsole");
        
        SubscribeLocalEvent<SSDStorageConsoleComponent, CryopodSSDStorageInteractWithItemEvent>(OnInteractWithItem);
        SubscribeLocalEvent<SSDStorageConsoleComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);
        SubscribeLocalEvent<SSDStorageConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        
        SubscribeLocalEvent<TransferredToCryoStorageEvent>(OnTransferredToCryo);
        
        SubscribeLocalEvent<SSDStorageConsoleComponent, GotEmaggedEvent>(OnEmagAct);
    }

    private void OnEmagAct(EntityUid uid, SSDStorageConsoleComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }


    /// <summary>
    /// Method for transferring entity to cryo storage
    /// </summary>
    /// <param name="uid"> Entity that haves SSDStorageConsoleComponent</param>
    /// <param name="target"> Entity to transfer</param>
    /// <param name="cryopodConsoleComp"> component</param>
    public void TransferToCryoStorage(EntityUid uid, EntityUid target, SSDStorageConsoleComponent? cryopodConsoleComp = null)
    {
        if (Resolve(uid, ref cryopodConsoleComp))
        {
            TransferToCryoStorage(uid, cryopodConsoleComp, target);
        }
    }
    
    private void OnInteractWithItem(EntityUid uid, SSDStorageConsoleComponent component, CryopodSSDStorageInteractWithItemEvent args)
    {
        if (args.Session.AttachedEntity is not EntityUid player)
            return;

        if (!Exists(args.InteractedItemUid))
        {
            _sawmill.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUid} stored in {ToPrettyString(uid)}");
            return;
        }

        if (!TryComp<ServerStorageComponent>(uid, out var storageComp))
        {
            return;
        }
        
        if (!_actionBlockerSystem.CanInteract(player, args.InteractedItemUid) || storageComp.Storage == null || !storageComp.Storage.Contains(args.InteractedItemUid))
            return;
        
        if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
            return;

        if (!_accessReaderSystem.IsAllowed(player, uid))
        {
            _sawmill.Info($"Player {ToPrettyString(player)} possibly exploits UI, trying to take item from {ToPrettyString(uid)} without access");
            return;
        }
        
        if (hands.ActiveHandEntity == null)
        {
            if (_handsSystem.TryPickupAnyHand(player, args.InteractedItemUid, handsComp: hands)
                && storageComp.StorageRemoveSound != null)
                _sawmill.Info($"{ToPrettyString(player)} takes {ToPrettyString(args.InteractedItemUid)} from {ToPrettyString(uid)}");
        }
    }

    /// <summary>
    /// System reacts to broadcast event
    /// first suitable CryoStorageConsole component will handle it
    /// </summary>
    /// <param name="args"> Event contains information about the cryopod and the entity,
    /// which we must transfer to the cryo storage</param>
    private void OnTransferredToCryo(TransferredToCryoStorageEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var entityEnumerator = EntityQueryEnumerator<SSDStorageConsoleComponent>();

        while (entityEnumerator.MoveNext(out var uid, out var ssdStorageConsoleComp))
        {
            if (ssdStorageConsoleComp.IsCryopod)
            {
                continue;
            }
            
            var consoleCoord = Transform(uid).Coordinates;
            var cryopodCoord = Transform(args.CryopodSSD).Coordinates;

            if (consoleCoord.InRange(_entityManager, _transformSystem, cryopodCoord, ssdStorageConsoleComp.RadiusToConnect))
            {
                args.Handled = true;
                TransferToCryoStorage(uid, ssdStorageConsoleComp, args.EntityToTransfer);
                return;
            }
        }
    }
    
    private void TransferToCryoStorage(EntityUid uid, SSDStorageConsoleComponent component, EntityUid entityToTransfer)
    {
        _sawmill.Info($"{ToPrettyString(entityToTransfer)} moved to cryo storage");

        var station = _stationSystem.GetOwningStation(uid);

        if (station is not null)
        {
            if (DeleteEntityRecord(entityToTransfer, station.Value, out var deletedRecord))
            {
                _chatSystem.DispatchStationAnnouncement(station.Value,
                    Loc.GetString(
                        "cryopodSSD-entered-cryo",
                        ("character", MetaData(entityToTransfer).EntityName),
                        ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(deletedRecord.JobTitle))),
                    Loc.GetString("cryopodSSD-sender"),
                    playSound: false);

                component.StoredEntities.Add(
                    $"{MetaData(entityToTransfer).EntityName} - [{deletedRecord.JobTitle}] - {_gameTiming.RealTime}");

                _stationJobsSystem.TryAdjustJobSlot(station.Value, deletedRecord.JobPrototype, 1);
            }
        }

        UndressEntity(uid, component, entityToTransfer);

        _entityManager.QueueDeleteEntity(entityToTransfer);

        ReplaceKillEntityObjectives(entityToTransfer);
    }

    /// <summary>
    /// Looks through all objectives in game,
    /// All KillPersonObjective where target equals to uid
    /// would be replaced with new random objective 
    /// </summary>
    /// <param name="uid"> target uid</param>
    private void ReplaceKillEntityObjectives(EntityUid uid)
    {
        foreach (var mind in _mindTrackerSystem.AllMinds)
        {
            if (mind.OwnedEntity is null)
            {
                continue;
            }
            
            IEnumerable<Objective> objectiveToReplace = mind.AllObjectives.Where(objective =>
                objective.Conditions.Any(condition => (condition as KillPersonCondition)?.IsTarget(uid) ?? false));

            foreach (var objective in objectiveToReplace)
            {
                mind.TryRemoveObjective(objective);
                var newObjective = _objectivesManager.GetRandomObjective(mind, "TraitorObjectiveGroups");
                if (newObjective is null || !mind.TryAddObjective(newObjective))
                {
                    _sawmill.Error($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he lost his objective and didn't get a new one");
                    continue;
                }
                    
                _sawmill.Info($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he get a new one");
            }
        }
    }
    
    /// <summary>
    /// Looking through all Entity's items,
    /// and if item is not in SSD storage whitelist - deletes it,
    /// otherwise transfers it to ssd storage
    /// </summary>
    /// <param name="uid"> EntityUid of our ssd storage</param>
    /// <param name="component"></param>
    /// <param name="target"> Entity to undress</param>

    private void UndressEntity(EntityUid uid, SSDStorageConsoleComponent component, EntityUid target)
    {
        if (!TryComp<ServerStorageComponent>(uid, out var storageComponent)
            || storageComponent.Storage is null)
        {
            return;
        }
        
        /*
        * It would be great if we could instantly delete items when we know they are not whitelisted.
        * However, this could lead to a situation where we accidentally delete the uniform,
        * resulting in all items inside the pockets being dropped before we add them to the itemsToTransfer list.
        * So we should have itemsToDelete list.
        */

        List<EntityUid> itemsToTransfer = new();
        List<EntityUid> itemsToDelete = new();

        // Looking through all 
        SortContainedItems(in target,ref itemsToTransfer,ref itemsToDelete, in component.Whitelist);

        foreach (var item in itemsToTransfer)
        {
            storageComponent.Storage.Insert(item);
        }

        foreach (var item in itemsToDelete)
        {
            _entityManager.DeleteEntity(item);
        }
    }

    /// <summary>
    /// Recursively goes through all child entities of our entity
    /// and if entity is item - adds it to whiteListedItems,
    /// otherwise adds it to itemsToDelete 
    /// </summary>
    /// <param name="storageToLook"></param>
    /// <param name="whitelistedItems"></param>
    /// <param name="itemsToDelete"></param>
    /// <param name="whitelist"></param>
    private void SortContainedItems(in EntityUid storageToLook, ref List<EntityUid> whitelistedItems,
        ref List<EntityUid> itemsToDelete, in EntityWhitelist? whitelist)
    {
        if (TryComp<TransformComponent>(storageToLook, out var transformComponent))
        {
            foreach (var childUid in transformComponent.ChildEntities)
            {
                if (!HasComp<ItemComponent>(childUid))
                {
                    continue;
                }

                if (whitelist is null || whitelist.IsValid(childUid))
                {
                    whitelistedItems.Add(childUid);
                }
                else
                {
                    itemsToDelete.Add(childUid);
                }

                // As far as I know, ChildEntities cannot be recursive 
                SortContainedItems(in childUid, ref whitelistedItems, ref itemsToDelete, in whitelist);
            }
        }
    }
    
    /// <summary>
    /// Delete entity records from station general records
    /// using DNA to match record
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="station"></param>
    /// <param name="deletedRecord"> returns copy of deleted generalRecord </param>
    /// <returns> True if we successfully deleted record of entity, otherwise returns false</returns>
    private bool DeleteEntityRecord(EntityUid uid, EntityUid station,[NotNullWhen(true)] out GeneralStationRecord? deletedRecord)
    {
        var stationRecord = FindEntityStationRecordKey(station, uid);

        deletedRecord = null;
        
        if (stationRecord is null)
        {
            return false;
        }

        deletedRecord = stationRecord.Value.Item2;

        _stationRecordsSystem.RemoveRecord(station, stationRecord.Value.Item1);
        
        return true;
    }
    
    private (StationRecordKey, GeneralStationRecord)? FindEntityStationRecordKey(EntityUid station, EntityUid uid)
    {
        if (TryComp<DnaComponent>(uid, out var dnaComponent))
        {
            var stationRecords = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(station);
            var result = stationRecords.FirstOrNull(records => records.Item2.DNA == dnaComponent.DNA);
            if (result is not null)
            {
                return result.Value;
            }
        }

        return null;
    }
    
    private void OnStorageItemRemoved(EntityUid uid, SSDStorageConsoleComponent storageComp, EntRemovedFromContainerMessage args)
    {
        
        UpdateUserInterface(uid, storageComp, args.Entity, true);
    }

    private void UpdateUserInterface(EntityUid uid, SSDStorageConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is null)
        {
            return;
        }
        UpdateUserInterface(uid, component, args.Session.AttachedEntity.Value);
    }

    private void UpdateUserInterface(EntityUid uid, SSDStorageConsoleComponent? component, EntityUid user,
        bool forseAccess = false)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }
        
        if (TryComp<ServerStorageComponent>(uid, out var storageComponent) && storageComponent.StoredEntities is not null)
        {
            var hasAccess = HasComp<EmaggedComponent>(uid) || _accessReaderSystem.IsAllowed(user, uid) || forseAccess;
            var storageState = hasAccess ?  
                new StorageBoundUserInterfaceState((List<EntityUid>) storageComponent.StoredEntities, 
                    storageComponent.StorageUsed, 
                    storageComponent.StorageCapacityMax)
                : new StorageBoundUserInterfaceState(new List<EntityUid>(),
                    0,
                    storageComponent.StorageCapacityMax);
            
            var state = new SSDStorageConsoleState(hasAccess, component.StoredEntities, storageState);
            SetStateForInterface(uid, state);
        }
    }
    
    private void SetStateForInterface(EntityUid uid, SSDStorageConsoleState storageConsoleState)
    {
        var ui = _userInterface.GetUiOrNull(uid, SSDStorageConsoleKey.Key);
        if (ui is not null)
        {
            _userInterface.SetUiState(ui, storageConsoleState);
        }
    }
}
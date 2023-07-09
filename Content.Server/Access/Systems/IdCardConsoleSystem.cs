using System.Linq;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardConsoleComponent, WriteToTargetIdMessage>(OnWriteToTargetIdMessage);

        // one day, maybe bound user interfaces can be shared too.
        SubscribeLocalEvent<IdCardConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
    }

    private void OnWriteToTargetIdMessage(EntityUid uid, IdCardConsoleComponent component, WriteToTargetIdMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        TryWriteToTargetId(uid, args.FullName, args.JobTitle, args.AccessList, args.JobPrototype, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, IdCardConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        string[]? possibleAccess = null;
        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdName = EntityManager.GetComponent<MetaDataComponent>(item).EntityName;
            possibleAccess = _accessReader.FindAccessTags(item).ToArray();
        }

        IdCardConsoleBoundUserInterfaceState newState;
        // this could be prettier
        if (component.TargetIdSlot.Item is not { Valid: true } targetId)
        {
            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                false,
                null,
                null,
                null,
                possibleAccess,
                string.Empty,
                privilegedIdName,
                string.Empty);
        }
        else
        {
            var targetIdComponent = EntityManager.GetComponent<IdCardComponent>(targetId);
            var targetAccessComponent = EntityManager.GetComponent<AccessComponent>(targetId);

            var jobProto = string.Empty;
            if (_station.GetOwningStation(uid) is { } station
                && EntityManager.TryGetComponent<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && keyStorage.Key != null
                && _record.TryGetRecord<GeneralStationRecord>(station, keyStorage.Key.Value, out var record))
            {
                jobProto = record.JobPrototype;
            }

            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                true,
                targetIdComponent.FullName,
                targetIdComponent.JobTitle,
                targetAccessComponent.Tags.ToArray(),
                possibleAccess,
                jobProto,
                privilegedIdName,
                EntityManager.GetComponent<MetaDataComponent>(targetId).EntityName);
        }

        _userInterface.TrySetUiState(uid, IdCardConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="IdCardConsoleComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetId(EntityUid uid,
        string newFullName,
        string newJobTitle,
        List<string> newAccessList,
        string newJobProto,
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        _idCard.TryChangeFullName(targetId, newFullName, player: player);
        _idCard.TryChangeJobTitle(targetId, newJobTitle, player: player);

        if (!newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var oldTags = _access.TryGetTags(targetId) ?? new List<string>();
        oldTags = oldTags.ToList();

        var privilegedId = component.PrivilegedIdSlot.Item;

        if (oldTags.SequenceEqual(newAccessList))
            return;

        // I hate that C# doesn't have an option for this and don't desire to write this out the hard way.
        // var difference = newAccessList.Difference(oldTags);
        var difference = (newAccessList.Union(oldTags)).Except(newAccessList.Intersect(oldTags)).ToHashSet();
        // NULL SAFETY: PrivilegedIdIsAuthorized checked this earlier.
        var privilegedPerms = _accessReader.FindAccessTags(privilegedId!.Value).ToHashSet();
        if (!difference.IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();
        _access.TrySetTags(targetId, newAccessList);

        /*TODO: ECS SharedIdCardConsoleComponent and then log on card ejection, together with the save.
        This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves AA*/
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(targetId):entity} with the following accesses: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");

        UpdateStationRecord(uid, targetId, newFullName, newJobTitle, newJobProto);
    }

    /// <summary>
    /// Returns true if there is an ID in <see cref="IdCardConsoleComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    /// <remarks>
    /// Other code relies on the fact this returns false if privileged Id is null. Don't break that invariant.
    /// </remarks>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (!EntityManager.TryGetComponent<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, reader);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, string newFullName, string newJobTitle, string newJobProto)
    {
        if (_station.GetOwningStation(uid) is not { } station
            || !EntityManager.TryGetComponent<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(station, key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;

        if (_prototype.TryIndex<JobPrototype>(newJobProto, out var job))
        {
            record.JobPrototype = newJobProto;
            record.JobIcon = job.Icon;
        }

        _record.Synchronize(station);
    }
}

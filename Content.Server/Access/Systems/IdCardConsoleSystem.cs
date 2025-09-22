using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Containers;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Chat; // Starlight-edit

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardConsoleComponent, WriteToTargetIdMessage>(OnWriteToTargetIdMessage);

        SubscribeLocalEvent<IdCardConsoleComponent, AccessGroupSelectedMessage>(OnAccessGroupSelected); // Starlight-edit

        // one day, maybe bound user interfaces can be shared too.
        SubscribeLocalEvent<IdCardConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, DamageChangedEvent>(OnDamageChanged);

        // Intercept the event before anyone can do anything with it!
        SubscribeLocalEvent<IdCardConsoleComponent, MachineDeconstructedEvent>(OnMachineDeconstructed,
            before: [typeof(EmptyOnMachineDeconstructSystem), typeof(ItemSlotsSystem)]);
    }

    private void OnWriteToTargetIdMessage(EntityUid uid, IdCardConsoleComponent component, WriteToTargetIdMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToTargetId(uid, args.FullName, args.JobTitle, args.AccessList, args.JobPrototype, player, component);

        UpdateUserInterface(uid, component, args);
    }

    // Starlight-start

    private void OnAccessGroupSelected(EntityUid uid, IdCardConsoleComponent component, IdCardConsoleComponent.AccessGroupSelectedMessage args)
    {
        component.CurrentAccessGroup = args.SelectedGroup;

        UpdateUserInterface(uid, component, args);
    }

    // Starlight-end

    private void UpdateUserInterface(EntityUid uid, IdCardConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        List<ProtoId<AccessLevelPrototype>> possibleAccess = new(); // Starlight
        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdName = Comp<MetaDataComponent>(item).EntityName;
        // Starlight-edit: Start
            var privilegedTags = _accessReader.FindAccessTags(item).ToHashSet();
            possibleAccess = privilegedTags.ToList();
        }
        else
        {
            privilegedIdName = string.Empty;
        }

        List<ProtoId<AccessGroupPrototype>> availableGroups = new();
        bool isHighPrivilege = false;
        
        if (possibleAccess.Count > 0)
        {
            var allPossibleAccess = _prototype.EnumeratePrototypes<AccessLevelPrototype>()
                .Where(a => a.CanAddToIdCard)
                .Select(a => a.ID)
                .ToHashSet();
            
            isHighPrivilege = possibleAccess.Count >= allPossibleAccess.Count * 0.8f;

            foreach (var groupId in component.AccessGroups.ToList())
            {
                if (!_prototype.TryIndex<AccessGroupPrototype>(groupId, out var groupPrototype))
                    continue;

                var groupTags = groupPrototype.Tags.Where(tag => 
                    _prototype.TryIndex<AccessLevelPrototype>(tag, out var accessProto) && 
                    accessProto.CanAddToIdCard).ToList();
                
                if (groupTags.Count == 0)
                    continue;
                
                var matchingTags = groupTags.Count(tag => possibleAccess.Contains(tag));
                var threshold = Math.Max(1, Math.Min(3, groupTags.Count / 2));
                
                if (matchingTags >= threshold)
                {
                    availableGroups.Add(groupId);
                }
            }
        }

        var currentGroup = component.CurrentAccessGroup;

        if (currentGroup == null || !availableGroups.Contains(currentGroup.Value))
        {
            if (availableGroups.Count > 0)
            {
                // Start on group selected
                var preferredGroups = new[] { "Command", "Security", "Engineering", "Medical" };
                var selectedGroup = preferredGroups
                    .Select(name => (ProtoId<AccessGroupPrototype>)name)
                    .FirstOrDefault(group => availableGroups.Contains(group));
                
                currentGroup = availableGroups.Contains(selectedGroup) ? selectedGroup : availableGroups.First();
            }
            else
            {
                currentGroup = component.AccessGroups.FirstOrDefault();
            }
            
            component.CurrentAccessGroup = currentGroup;
        }

        var showGroups = availableGroups.Count > 1;
        // Starlight-edit: End

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
                string.Empty,
                // Starlight-edit: Start
                currentGroup.HasValue ? currentGroup.Value : component.AccessGroups.FirstOrDefault(),
                availableGroups); 
                // Starlight-edit: End
        }
        else
        {
            var targetIdComponent = Comp<IdCardComponent>(targetId);
            var targetAccessComponent = Comp<AccessComponent>(targetId);

            var jobProto = targetIdComponent.JobPrototype ?? new ProtoId<JobPrototype>(string.Empty);
            if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && keyStorage.Key is { } key
                && _record.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                jobProto = record.JobPrototype;
            }

            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                true,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                targetAccessComponent.Tags.ToList(),
                possibleAccess,
                jobProto,
                privilegedIdName,
                Name(targetId),
                // Starlight-edit: Start
                currentGroup.HasValue ? currentGroup.Value : component.AccessGroups.FirstOrDefault(),
                availableGroups);
                // Starlight-edit: End
        }

        _userInterface.SetUiState(uid, IdCardConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="IdCardConsoleComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetId(EntityUid uid,
        string newFullName,
        string newJobTitle,
        List<ProtoId<AccessLevelPrototype>> newAccessList,
        ProtoId<JobPrototype> newJobProto,
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        _idCard.TryChangeFullName(targetId, newFullName, player: player);
        _idCard.TryChangeJobTitle(targetId, newJobTitle, player: player);

        if (_prototype.TryIndex<JobPrototype>(newJobProto, out var job)
            && _prototype.Resolve(job.Icon, out var jobIcon))
        {
            _idCard.TryChangeJobIcon(targetId, jobIcon, player: player);
            _idCard.TryChangeJobDepartment(targetId, job);
        }

        UpdateStationRecord(uid, targetId, newFullName, newJobTitle, job);
        if ((!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out _))
            && newJobProto != string.Empty)
        {
            Comp<IdCardComponent>(targetId).JobPrototype = newJobProto;
        }

        // Starlight-start

        var currentGroup = component.CurrentAccessGroup ?? component.AccessGroups.FirstOrDefault();
        if (!_prototype.TryIndex<AccessGroupPrototype>(currentGroup, out var currentGroupPrototype))
        {
            _sawmill.Warning($"Current access group {currentGroup} not found!");
            return;
        }

       var oldTags = _access.TryGetTags(targetId)?.ToHashSet() ?? new HashSet<ProtoId<AccessLevelPrototype>>();

        var groupTags = currentGroupPrototype.Tags.ToHashSet();

        var oldGroupTags = oldTags.Intersect(groupTags).ToHashSet();
        var newGroupTags = newAccessList.Intersect(groupTags).ToHashSet();

        // Starlight-end

        // Ensure the user isn't trying to add access they shouldn't be able to.
        // Starlight-start: Change to access groups
        List<ProtoId<AccessLevelPrototype>> Accesses = new();
        foreach (var group in component.AccessGroups.ToList())
        {
            if (!_prototype.TryIndex<AccessGroupPrototype>(group, out var groupPrototype))
                continue;

            Accesses.AddRange(groupPrototype.Tags.ToList());
        }
        // Starlight-end
        if (!newAccessList.TrueForAll(x => Accesses.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var privilegedId = component.PrivilegedIdSlot.Item;

        var finalTags = oldTags.Except(groupTags).Union(newGroupTags); // Starlight-edit

        if (oldTags.SetEquals(finalTags)) // Starlight-edit
            return;

        // I hate that C# doesn't have an option for this and don't desire to write this out the hard way.
        // var difference = newAccessList.Difference(oldTags);
        var difference = finalTags.Union(oldTags).Except(finalTags.Intersect(oldTags)).ToHashSet(); // Starlight-edit
        // NULL SAFETY: PrivilegedIdIsAuthorized checked this earlier.
        var privilegedPerms = _accessReader.FindAccessTags(privilegedId!.Value).ToHashSet();
        if (!difference.IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        /*TODO: ECS SharedIdCardConsoleComponent and then log on card ejection, together with the save.
        This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves AA*/
        // Starlight-edit: Start
        var addedTags = finalTags.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(finalTags).Select(tag => "-" + tag).ToList();
        _access.TrySetTags(targetId, finalTags);
        // Starlight-edit: End

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(targetId):entity} with the following accesses: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");
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

        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, reader);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, string newFullName, ProtoId<AccessLevelPrototype> newJobTitle, JobPrototype? newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;

        if (newJobProto != null)
        {
            record.JobPrototype = newJobProto.ID;
            record.JobIcon = newJobProto.Icon;
        }

        _record.Synchronize(key);
    }

    private void OnMachineDeconstructed(Entity<IdCardConsoleComponent> entity, ref MachineDeconstructedEvent args)
    {
        TryDropAndThrowIds(entity.AsNullable());
    }

    private void OnDamageChanged(Entity<IdCardConsoleComponent> entity, ref DamageChangedEvent args)
    {
        if (TryDropAndThrowIds(entity.AsNullable()))
            _chat.TrySendInGameICMessage(entity, Loc.GetString("id-card-console-damaged"), InGameICChatType.Speak, true);
    }

    #region PublicAPI

    /// <summary>
    ///     Tries to drop any IDs stored in the console, and then tries to throw them away.
    ///     Returns true if anything was ejected and false otherwise.
    /// </summary>
    public bool TryDropAndThrowIds(Entity<IdCardConsoleComponent?, ItemSlotsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        var didEject = false;

        foreach (var slot in ent.Comp2.Slots.Values)
        {
            if (slot.Item == null || slot.ContainerSlot == null)
                continue;

            var item = slot.Item.Value;
            if (_container.Remove(item, slot.ContainerSlot))
            {
                _throwing.TryThrow(item, _random.NextVector2(), baseThrowSpeed: 5f);
                didEject = true;
            }
        }

        return didEject;
    }

    #endregion
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.NameIdentifier;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Access.Systems;

public sealed class AccessReaderSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStationRecordsSystem _recordsSystem = default!;

    private static readonly ProtoId<TagPrototype> PreventAccessLoggingTag = "PreventAccessLogging";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessReaderComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<AccessReaderComponent, LinkAttemptEvent>(OnLinkAttempt);

        SubscribeLocalEvent<AccessReaderComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AccessReaderComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, AccessReaderComponent component, ref ComponentGetState args)
    {
        args.State = new AccessReaderComponentState(component.Enabled, component.DenyTags, component.AccessLists,
            _recordsSystem.Convert(component.AccessKeys), component.AccessLog, component.AccessLogLimit);
    }

    private void OnHandleState(EntityUid uid, AccessReaderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AccessReaderComponentState state)
            return;
        component.Enabled = state.Enabled;
        component.AccessKeys.Clear();
        foreach (var key in state.AccessKeys)
        {
            var id = EnsureEntity<AccessReaderComponent>(key.Item1, uid);
            if (!id.IsValid())
                continue;

            component.AccessKeys.Add(new StationRecordKey(key.Item2, id));
        }

        component.AccessLists = new(state.AccessLists);
        component.DenyTags = new(state.DenyTags);
        component.AccessLog = new(state.AccessLog);
        component.AccessLogLimit = state.AccessLogLimit;
    }

    private void OnLinkAttempt(EntityUid uid, AccessReaderComponent component, LinkAttemptEvent args)
    {
        if (args.User == null) // AutoLink (and presumably future external linkers) have no user.
            return;
        if (!IsAllowed(args.User.Value, uid, component))
            args.Cancel();
    }

    private void OnEmagged(EntityUid uid, AccessReaderComponent reader, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (!reader.BreakOnAccessBreaker)
            return;

        if (!GetMainAccessReader(uid, out var accessReader))
            return;

        if (accessReader.Value.Comp.AccessLists.Count < 1)
            return;

        args.Repeatable = true;
        args.Handled = true;
        accessReader.Value.Comp.AccessLists.Clear();
        accessReader.Value.Comp.AccessLog.Clear();
        Dirty(uid, reader);
    }

    /// <summary>
    /// Searches the source for access tags
    /// then compares it with the all targets accesses to see if it is allowed.
    /// </summary>
    /// <param name="user">The entity that wants access.</param>
    /// <param name="target">The entity to search for an access reader</param>
    /// <param name="reader">Optional reader from the target entity</param>
    public bool IsAllowed(EntityUid user, EntityUid target, AccessReaderComponent? reader = null)
    {
        if (!Resolve(target, ref reader, false))
            return true;

        if (!reader.Enabled)
            return true;

        var accessSources = FindPotentialAccessItems(user);
        var access = FindAccessTags(user, accessSources);
        FindStationRecordKeys(user, out var stationKeys, accessSources);

        if (!IsAllowed(access, stationKeys, target, reader))
            return false;

        if (!_tag.HasTag(user, PreventAccessLoggingTag))
            LogAccess((target, reader), user);

        return true;
    }

    /// <summary>
    /// Searches an entity for an access reader. This is either the entity itself or an entity in its <see cref="AccessReaderComponent.ContainerAccessProvider"/>.
    /// </summary>
    /// <param name="uid">The entity being searched for an access reader.</param>
    /// <param name="ent">The returned access reader entity.</param>
    public bool GetMainAccessReader(EntityUid uid, [NotNullWhen(true)] out Entity<AccessReaderComponent>? ent)
    {
        ent = null;
        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return false;

        ent = (uid, accessReader);

        if (ent.Value.Comp.ContainerAccessProvider == null)
            return true;

        if (!_containerSystem.TryGetContainer(uid, ent.Value.Comp.ContainerAccessProvider, out var container))
            return true;

        foreach (var entity in container.ContainedEntities)
        {
            if (TryComp<AccessReaderComponent>(entity, out var containedReader))
            {
                ent = (entity, containedReader);
                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Check whether the given access permissions satisfy an access reader's requirements.
    /// </summary>
    /// <param name="access">A collection of access permissions being used on the access reader.</param>
    /// <param name="stationKeys">A collection of station record keys being used on the access reader.</param>
    /// <param name="target">The entity being checked.</param>
    /// <param name="reader">The access reader being checked.</param>
    public bool IsAllowed(
        ICollection<ProtoId<AccessLevelPrototype>> access,
        ICollection<StationRecordKey> stationKeys,
        EntityUid target,
        AccessReaderComponent reader)
    {
        if (!reader.Enabled)
            return true;

        if (reader.ContainerAccessProvider == null)
            return IsAllowedInternal(access, stationKeys, reader);

        if (!_containerSystem.TryGetContainer(target, reader.ContainerAccessProvider, out var container))
            return false;

        // If entity is paused then always allow it at this point.
        // Door electronics is kind of a mess but yeah, it should only be an unpaused ent interacting with it
        if (Paused(target))
            return true;

        foreach (var entity in container.ContainedEntities)
        {
            if (!TryComp(entity, out AccessReaderComponent? containedReader))
                continue;

            if (IsAllowed(access, stationKeys, entity, containedReader))
                return true;
        }

        return false;
    }

    private bool IsAllowedInternal(ICollection<ProtoId<AccessLevelPrototype>> access, ICollection<StationRecordKey> stationKeys, AccessReaderComponent reader)
    {
        return !reader.Enabled
               || AreAccessTagsAllowed(access, reader)
               || AreStationRecordKeysAllowed(stationKeys, reader);
    }

    /// <summary>
    /// Compares the given tags with the readers access list to see if it is allowed.
    /// </summary>
    /// <param name="accessTags">A list of access tags.</param>
    /// <param name="reader">The access reader to check against.</param>
    public bool AreAccessTagsAllowed(ICollection<ProtoId<AccessLevelPrototype>> accessTags, AccessReaderComponent reader)
    {
        if (reader.DenyTags.Overlaps(accessTags))
        {
            // Sec owned by cargo.

            // Note that in resolving the issue with only one specific item "counting" for access, this became a bit more strict.
            // As having an ID card in any slot that "counts" with a denied access group will cause denial of access.
            // DenyTags doesn't seem to be used right now anyway, though, so it'll be dependent on whoever uses it to figure out if this matters.
            return false;
        }

        if (reader.AccessLists.Count == 0)
            return true;

        foreach (var set in reader.AccessLists)
        {
            if (set.IsSubsetOf(accessTags))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compares the given stationrecordkeys with the accessreader to see if it is allowed.
    /// </summary>
    /// <param name="keys">The collection of station record keys being used against the access reader.</param>
    /// <param name="reader">The access reader that is being checked.</param>
    public bool AreStationRecordKeysAllowed(ICollection<StationRecordKey> keys, AccessReaderComponent reader)
    {
        foreach (var key in reader.AccessKeys)
        {
            if (keys.Contains(key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds all the items that could potentially give access to an entity.
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    public HashSet<EntityUid> FindPotentialAccessItems(EntityUid uid)
    {
        FindAccessItemsInventory(uid, out var items);

        var ev = new GetAdditionalAccessEvent
        {
            Entities = items
        };
        RaiseLocalEvent(uid, ref ev);

        foreach (var item in new ValueList<EntityUid>(items))
        {
            items.UnionWith(FindPotentialAccessItems(item));
        }
        items.Add(uid);
        return items;
    }

    /// <summary>
    /// Finds the access tags on an entity.
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
    public ICollection<ProtoId<AccessLevelPrototype>> FindAccessTags(EntityUid uid, HashSet<EntityUid>? items = null)
    {
        HashSet<ProtoId<AccessLevelPrototype>>? tags = null;
        var owned = false;

        items ??= FindPotentialAccessItems(uid);

        foreach (var ent in items)
        {
            FindAccessTagsItem(ent, ref tags, ref owned);
        }

        return (ICollection<ProtoId<AccessLevelPrototype>>?)tags ?? Array.Empty<ProtoId<AccessLevelPrototype>>();
    }

    /// <summary>
    /// Finds any station record keys on an entity.
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="recordKeys">A collection of the station record keys that were found.</param>
    /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
    public bool FindStationRecordKeys(EntityUid uid, out ICollection<StationRecordKey> recordKeys, HashSet<EntityUid>? items = null)
    {
        recordKeys = new HashSet<StationRecordKey>();

        items ??= FindPotentialAccessItems(uid);

        foreach (var ent in items)
        {
            if (FindStationRecordKeyItem(ent, out var key))
                recordKeys.Add(key.Value);
        }

        return recordKeys.Any();
    }

    /// <summary>
    /// Try to find <see cref="AccessComponent"/> on this item or inside this item (if it's a PDA).
    /// This version merges into a set or replaces the set.
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="tags">The access tags being merged or replaced.</param>
    /// <param name="owned">If true, the tags will be merged. Otherwise they are replaced.</param>
    private void FindAccessTagsItem(EntityUid uid, ref HashSet<ProtoId<AccessLevelPrototype>>? tags, ref bool owned)
    {
        if (!FindAccessTagsItem(uid, out var targetTags))
        {
            // no tags, no problem
            return;
        }
        if (tags != null)
        {
            // existing tags, so copy to make sure we own them
            if (!owned)
            {
                tags = new(tags);
                owned = true;
            }
            // then merge
            tags.UnionWith(targetTags);
        }
        else
        {
            // no existing tags, so now they're ours
            tags = targetTags;
            owned = false;
        }
    }

    #region: AccessLists API

    /// <summary>
    /// Clears the entity's <see cref="AccessReaderComponent.AccessLists"/>.
    /// </summary>
    /// <param name="ent">The access reader entity which is having its access permissions cleared.</param>
    public void ClearAccesses(Entity<AccessReaderComponent> ent)
    {
        ent.Comp.AccessLists.Clear();

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <summary>
    /// Replaces the access permissions in an entity's <see cref="AccessReaderComponent.AccessLists"/> with a supplied list.
    /// </summary>
    /// <param name="ent">The access reader entity which is having its list of access permissions replaced.</param>
    /// <param name="accesses">The list of access permissions replacing the original one.</param>
    public void SetAccesses(Entity<AccessReaderComponent> ent, List<HashSet<ProtoId<AccessLevelPrototype>>> accesses)
    {
        ent.Comp.AccessLists.Clear();

        AddAccesses(ent, accesses);
    }

    /// <inheritdoc cref = "SetAccesses"/>
    public void SetAccesses(Entity<AccessReaderComponent> ent, List<ProtoId<AccessLevelPrototype>> accesses)
    {
        ent.Comp.AccessLists.Clear();

        AddAccesses(ent, accesses);
    }

    /// <summary>
    /// Adds a collection of access permissions to an access reader entity's <see cref="AccessReaderComponent.AccessLists"/>
    /// </summary>
    /// <param name="ent">The access reader entity to which the new access permissions are being added.</param>
    /// <param name="accesses">The list of access permissions being added.</param>
    public void AddAccesses(Entity<AccessReaderComponent> ent, List<HashSet<ProtoId<AccessLevelPrototype>>> accesses)
    {
        foreach (var access in accesses)
        {
            AddAccess(ent, access, false);
        }

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <inheritdoc cref = "AddAccesses"/>
    public void AddAccesses(Entity<AccessReaderComponent> ent, List<ProtoId<AccessLevelPrototype>> accesses)
    {
        foreach (var access in accesses)
        {
            AddAccess(ent, access, false);
        }

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <summary>
    /// Adds an access permission to an access reader entity's <see cref="AccessReaderComponent.AccessLists"/>
    /// </summary>
    /// <param name="ent">The access reader entity to which the access permission is being added.</param>
    /// <param name="access">The access permission being added.</param>
    /// <param name="dirty">If true, the component will be  marked as changed afterward.</param>
    public void AddAccess(Entity<AccessReaderComponent> ent, HashSet<ProtoId<AccessLevelPrototype>> access, bool dirty = true)
    {
        ent.Comp.AccessLists.Add(access);

        if (!dirty)
            return;

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <inheritdoc cref = "AddAccess"/>
    public void AddAccess(Entity<AccessReaderComponent> ent, ProtoId<AccessLevelPrototype> access, bool dirty = true)
    {
        AddAccess(ent, new HashSet<ProtoId<AccessLevelPrototype>>() { access }, dirty);
    }

    /// <summary>
    /// Removes a collection of access permissions from an access reader entity's <see cref="AccessReaderComponent.AccessLists"/>
    /// </summary>
    /// <param name="ent">The access reader entity from which the access permissions are being removed.</param>
    /// <param name="accesses">The list of access permissions being removed.</param>
    public void RemoveAccesses(Entity<AccessReaderComponent> ent, List<HashSet<ProtoId<AccessLevelPrototype>>> accesses)
    {
        foreach (var access in accesses)
        {
            RemoveAccess(ent, access, false);
        }

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <inheritdoc cref = "RemoveAccesses"/>
    public void RemoveAccesses(Entity<AccessReaderComponent> ent, List<ProtoId<AccessLevelPrototype>> accesses)
    {
        foreach (var access in accesses)
        {
            RemoveAccess(ent, access, false);
        }

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <summary>
    /// Removes an access permission from an access reader entity's <see cref="AccessReaderComponent.AccessLists"/>
    /// </summary>
    /// <param name="ent">The access reader entity from which the access permission is being removed.</param>
    /// <param name="access">The access permission being removed.</param>
    /// <param name="dirty">If true, the component will be marked as changed afterward.</param>
    public void RemoveAccess(Entity<AccessReaderComponent> ent, HashSet<ProtoId<AccessLevelPrototype>> access, bool dirty = true)
    {
        for (int i = ent.Comp.AccessLists.Count - 1; i >= 0; i--)
        {
            if (ent.Comp.AccessLists[i].SetEquals(access))
            {
                ent.Comp.AccessLists.RemoveAt(i);
            }
        }

        if (!dirty)
            return;

        Dirty(ent);
        RaiseLocalEvent(ent, new AccessReaderConfigurationChangedEvent());
    }

    /// <inheritdoc cref = "RemoveAccess"/>
    public void RemoveAccess(Entity<AccessReaderComponent> ent, ProtoId<AccessLevelPrototype> access, bool dirty = true)
    {
        RemoveAccess(ent, new HashSet<ProtoId<AccessLevelPrototype>>() { access }, dirty);
    }

    #endregion

    #region: AccessKeys API

    /// <summary>
    /// Clears all access keys from an access reader.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    public void ClearAccessKeys(Entity<AccessReaderComponent> ent)
    {
        ent.Comp.AccessKeys.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Replaces all access keys on an access reader with those from a supplied list.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="keys">The new access keys that are replacing the old ones.</param>
    public void SetAccessKeys(Entity<AccessReaderComponent> ent, HashSet<StationRecordKey> keys)
    {
        ent.Comp.AccessKeys.Clear();

        foreach (var key in keys)
        {
            ent.Comp.AccessKeys.Add(key);
        }

        Dirty(ent);
    }

    /// <summary>
    /// Adds an access key to an access reader.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="key">The access key being added.</param>
    public void AddAccessKey(Entity<AccessReaderComponent> ent, StationRecordKey key)
    {
        ent.Comp.AccessKeys.Add(key);
        Dirty(ent);
    }

    /// <summary>
    /// Removes an access key from an access reader.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="key">The access key being removed.</param>
    public void RemoveAccessKey(Entity<AccessReaderComponent> ent, StationRecordKey key)
    {
        ent.Comp.AccessKeys.Remove(key);
        Dirty(ent);
    }

    #endregion

    #region: DenyTags API

    /// <summary>
    /// Clears all deny tags from an access reader.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    public void ClearDenyTags(Entity<AccessReaderComponent> ent)
    {
        ent.Comp.DenyTags.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Replaces all deny tags on an access reader with those from a supplied list.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="tag">The new tags that are replacing the old.</param>
    public void SetDenyTags(Entity<AccessReaderComponent> ent, HashSet<ProtoId<AccessLevelPrototype>> tags)
    {
        ent.Comp.DenyTags.Clear();

        foreach (var tag in tags)
        {
            ent.Comp.DenyTags.Add(tag);
        }

        Dirty(ent);
    }

    /// <summary>
    /// Adds a tag to an access reader that will be used to deny access.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="tag">The tag being added.</param>
    public void AddDenyTag(Entity<AccessReaderComponent> ent, ProtoId<AccessLevelPrototype> tag)
    {
        ent.Comp.DenyTags.Add(tag);
        Dirty(ent);
    }

    /// <summary>
    /// Removes a tag from an access reader that denied a user access.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="tag">The tag being removed.</param>
    public void RemoveDenyTag(Entity<AccessReaderComponent> ent, ProtoId<AccessLevelPrototype> tag)
    {
        ent.Comp.DenyTags.Remove(tag);
        Dirty(ent);
    }

    #endregion

    /// <summary>
    /// Enables/disables the access reader on an entity.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="enabled">Enable/disable the access reader.</param>
    public void SetActive(Entity<AccessReaderComponent> ent, bool enabled)
    {
        ent.Comp.Enabled = enabled;
        Dirty(ent);
    }

    /// <summary>
    /// Enables/disables the logging of access attempts on an access reader entity.
    /// </summary>
    /// <param name="ent">The access reader entity.</param>
    /// <param name="enabled">Enable/disable logging.</param>
    public void SetLoggingActive(Entity<AccessReaderComponent> ent, bool enabled)
    {
        ent.Comp.LoggingDisabled = !enabled;
        Dirty(ent);
    }

    /// <summary>
    /// Searches an entity's hand and ID slot for any contained items.
    /// </summary>
    /// <param name="uid">The entity being searched.</param>
    /// <param name="items">The collection of found items.</param>
    /// <returns>True if one or more items were found.</returns>
    public bool FindAccessItemsInventory(EntityUid uid, out HashSet<EntityUid> items)
    {
        items = new(_handsSystem.EnumerateHeld(uid));

        // maybe its inside an inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            items.Add(idUid.Value);
        }

        return items.Any();
    }

    /// <summary>
    /// Try to find <see cref="AccessComponent"/> on this entity or inside it (if it's a PDA).
    /// </summary>
    /// <param name="uid">The entity being searched.</param>
    /// <param name="tags">The access tags that were found.</param>
    /// <returns>True if one or more access tags were found.</returns>
    private bool FindAccessTagsItem(EntityUid uid, out HashSet<ProtoId<AccessLevelPrototype>> tags)
    {
        tags = new();
        var ev = new GetAccessTagsEvent(tags, _prototype);
        RaiseLocalEvent(uid, ref ev);

        return tags.Count != 0;
    }

    /// <summary>
    /// Try to find <see cref="StationRecordKeyStorageComponent"/> on this entity or inside it (if it's a PDA).
    /// </summary>
    /// <param name="uid">The entity being searched.</param>
    /// <param name="key">The station record key that was found.</param>
    /// <returns>True if a station record key was found.</returns>
    private bool FindStationRecordKeyItem(EntityUid uid, [NotNullWhen(true)] out StationRecordKey? key)
    {
        if (TryComp(uid, out StationRecordKeyStorageComponent? storage) && storage.Key != null)
        {
            key = storage.Key;
            return true;
        }

        if (TryComp<PdaComponent>(uid, out var pda) &&
            pda.ContainedId is { Valid: true } id)
        {
            if (TryComp<StationRecordKeyStorageComponent>(id, out var pdastorage) && pdastorage.Key != null)
            {
                key = pdastorage.Key;
                return true;
            }
        }

        key = null;
        return false;
    }

    /// <summary>
    /// Logs an access for a specific entity.
    /// </summary>
    /// <param name="ent">The reader to log the access on</param>
    /// <param name="accessor">The accessor to log</param>
    public void LogAccess(Entity<AccessReaderComponent> ent, EntityUid accessor)
    {
        if (IsPaused(ent) || ent.Comp.LoggingDisabled)
            return;

        string? name = null;
        if (TryComp<NameIdentifierComponent>(accessor, out var nameIdentifier))
            name = nameIdentifier.FullIdentifier;

        // TODO pass the ID card on IsAllowed() instead of using this expensive method
        // Set name if the accessor has a card and that card has a name and allows itself to be recorded
        var getIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, accessor, true);
        RaiseLocalEvent(getIdentityShortInfoEvent);
        if (getIdentityShortInfoEvent.Title != null)
        {
            name = getIdentityShortInfoEvent.Title;
        }

        LogAccess(ent, name ?? Loc.GetString("access-reader-unknown-id"));
    }

    /// <summary>
    /// Logs an access with a predetermined name
    /// </summary>
    /// <param name="ent">The reader to log the access on</param>
    /// <param name="name">The name to log as</param>
    public void LogAccess(Entity<AccessReaderComponent> ent, string name, TimeSpan? accessTime = null, bool force = false)
    {
        if (!force)
        {
            if (IsPaused(ent) || ent.Comp.LoggingDisabled)
                return;

            if (ent.Comp.AccessLog.Count >= ent.Comp.AccessLogLimit)
                ent.Comp.AccessLog.Dequeue();
        }

        var stationTime = accessTime ?? _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        ent.Comp.AccessLog.Enqueue(new AccessRecord(stationTime, name));

        Dirty(ent);
    }
}

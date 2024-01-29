using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Content.Shared.GameTicking;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Access.Systems;

public sealed class AccessReaderSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStationRecordsSystem _recordsSystem = default!;

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
        if (!HasComp<EmaggedComponent>(uid) && !IsAllowed(args.User.Value, uid, component))
            args.Cancel();
    }

    private void OnEmagged(EntityUid uid, AccessReaderComponent reader, ref GotEmaggedEvent args)
    {
        if (!reader.BreakOnEmag)
            return;
        args.Handled = true;
        reader.Enabled = false;
        reader.AccessLog.Clear();
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

        if (IsAllowed(access, stationKeys, target, reader))
        {
            LogAccess((target, reader), user);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check whether the given access permissions satisfy an access reader's requirements.
    /// </summary>
    public bool IsAllowed(
        ICollection<string> access,
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

        foreach (var entity in container.ContainedEntities)
        {
            if (!TryComp(entity, out AccessReaderComponent? containedReader))
                continue;

            if (IsAllowed(access, stationKeys, entity, containedReader))
                return true;
        }

        return false;
    }

    private bool IsAllowedInternal(ICollection<string> access, ICollection<StationRecordKey> stationKeys, AccessReaderComponent reader)
    {
        return !reader.Enabled
               || AreAccessTagsAllowed(access, reader)
               || AreStationRecordKeysAllowed(stationKeys, reader);
    }

    /// <summary>
    /// Compares the given tags with the readers access list to see if it is allowed.
    /// </summary>
    /// <param name="accessTags">A list of access tags</param>
    /// <param name="reader">An access reader to check against</param>
    public bool AreAccessTagsAllowed(ICollection<string> accessTags, AccessReaderComponent reader)
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
    /// Finds all the items that could potentially give access to a given entity
    /// </summary>
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
    /// Finds the access tags on the given entity
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
    public ICollection<string> FindAccessTags(EntityUid uid, HashSet<EntityUid>? items = null)
    {
        HashSet<string>? tags = null;
        var owned = false;

        items ??= FindPotentialAccessItems(uid);

        foreach (var ent in items)
        {
            FindAccessTagsItem(ent, ref tags, ref owned);
        }

        return (ICollection<string>?) tags ?? Array.Empty<string>();
    }

    /// <summary>
    /// Finds the access tags on the given entity
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="recordKeys"></param>
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
    ///     Try to find <see cref="AccessComponent"/> on this item
    ///     or inside this item (if it's pda)
    ///     This version merges into a set or replaces the set.
    ///     If owned is false, the existing tag-set "isn't ours" and can't be merged with (is read-only).
    /// </summary>
    private void FindAccessTagsItem(EntityUid uid, ref HashSet<string>? tags, ref bool owned)
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

    public bool FindAccessItemsInventory(EntityUid uid, out HashSet<EntityUid> items)
    {
        items = new();

        foreach (var item in _handsSystem.EnumerateHeld(uid))
        {
            items.Add(item);
        }

        // maybe its inside an inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            items.Add(idUid.Value);
        }

        return items.Any();
    }

    /// <summary>
    ///     Try to find <see cref="AccessComponent"/> on this item
    ///     or inside this item (if it's pda)
    /// </summary>
    private bool FindAccessTagsItem(EntityUid uid, out HashSet<string> tags)
    {
        tags = new();
        var ev = new GetAccessTagsEvent(tags, _prototype);
        RaiseLocalEvent(uid, ref ev);

        return tags.Count != 0;
    }

    /// <summary>
    ///     Try to find <see cref="StationRecordKeyStorageComponent"/> on this item
    ///     or inside this item (if it's pda)
    /// </summary>
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
    /// Logs an access
    /// </summary>
    /// <param name="ent">The reader to log the access on</param>
    /// <param name="accessor">The accessor to log</param>
    private void LogAccess(Entity<AccessReaderComponent> ent, EntityUid accessor)
    {
        if (IsPaused(ent))
            return;

        if (ent.Comp.AccessLog.Count >= ent.Comp.AccessLogLimit)
            ent.Comp.AccessLog.Dequeue();

        string? name = null;
        // TODO pass the ID card on IsAllowed() instead of using this expensive method
        // Set name if the accessor has a card and that card has a name and allows itself to be recorded
        if (_idCardSystem.TryFindIdCard(accessor, out var idCard)
            && idCard.Comp is { BypassLogging: false, FullName: not null })
            name = idCard.Comp.FullName;

        name ??= Loc.GetString("access-reader-unknown-id");

        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        ent.Comp.AccessLog.Enqueue(new AccessRecord(stationTime, name));
    }
}

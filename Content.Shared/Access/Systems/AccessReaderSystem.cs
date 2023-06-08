using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.StationRecords;
using Robust.Shared.GameStates;

namespace Content.Shared.Access.Systems
{
    public sealed class AccessReaderSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AccessReaderComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<AccessReaderComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<AccessReaderComponent, LinkAttemptEvent>(OnLinkAttempt);

            SubscribeLocalEvent<AccessReaderComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<AccessReaderComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, AccessReaderComponent component, ref ComponentGetState args)
        {
            args.State = new AccessReaderComponentState(component.Enabled, component.DenyTags, component.AccessLists,
                component.AccessKeys);
        }

        private void OnHandleState(EntityUid uid, AccessReaderComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not AccessReaderComponentState state)
                return;
            component.Enabled = state.Enabled;
            component.AccessKeys = new (state.AccessKeys);
            component.AccessLists = new (state.AccessLists);
            component.DenyTags = new (state.DenyTags);
        }

        private void OnLinkAttempt(EntityUid uid, AccessReaderComponent component, LinkAttemptEvent args)
        {
            if (args.User == null) // AutoLink (and presumably future external linkers) have no user.
                return;
            if (!HasComp<EmaggedComponent>(uid) && !IsAllowed(args.User.Value, component))
                args.Cancel();
        }

        private void OnInit(EntityUid uid, AccessReaderComponent reader, ComponentInit args)
        {
            var allTags = reader.AccessLists.SelectMany(c => c).Union(reader.DenyTags);
            foreach (var level in allTags)
            {
                if (!_prototypeManager.HasIndex<AccessLevelPrototype>(level))
                {
                    Logger.ErrorS("access", $"Invalid access level: {level}");
                }
            }
        }

        private void OnEmagged(EntityUid uid, AccessReaderComponent reader, ref GotEmaggedEvent args)
        {
            args.Handled = true;
            reader.Enabled = false;
            Dirty(reader);
        }

        /// <summary>
        /// Searches the source for access tags
        /// then compares it with the targets readers access list to see if it is allowed.
        /// </summary>
        /// <param name="source">The entity that wants access.</param>
        /// <param name="target">The entity to search for an access reader</param>
        /// <param name="reader">Optional reader from the target entity</param>
        public bool IsAllowed(EntityUid source, EntityUid target, AccessReaderComponent? reader = null)
        {
            if (!Resolve(target, ref reader, false))
                return true;
            return IsAllowed(source, reader);
        }

        /// <summary>
        /// Searches the given entity for access tags
        /// then compares it with the readers access list to see if it is allowed.
        /// </summary>
        /// <param name="entity">The entity that wants access.</param>
        /// <param name="reader">A reader from a different entity</param>
        public bool IsAllowed(EntityUid entity, AccessReaderComponent reader)
        {
            var allEnts = FindPotentialAccessItems(entity);

            // Access reader is totally disabled, so access is always allowed.
            if (!reader.Enabled)
                return true;

            if (AreAccessTagsAllowed(FindAccessTags(entity, allEnts), reader))
                return true;

            if (AreStationRecordKeysAllowed(FindStationRecordKeys(entity, allEnts), reader))
                return true;

            return false;
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

            return reader.AccessLists.Count == 0 || reader.AccessLists.Any(a => a.IsSubsetOf(accessTags));
        }

        /// <summary>
        /// Compares the given stationrecordkeys with the accessreader to see if it is allowed.
        /// </summary>
        public bool AreStationRecordKeysAllowed(ICollection<StationRecordKey> keys, AccessReaderComponent reader)
        {
            return keys.Any() && reader.AccessKeys.Any(keys.Contains);
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
        /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
        public ICollection<StationRecordKey> FindStationRecordKeys(EntityUid uid, HashSet<EntityUid>? items = null)
        {
            HashSet<StationRecordKey> keys = new();

            items ??= FindPotentialAccessItems(uid);

            foreach (var ent in items)
            {
                if (FindStationRecordKeyItem(ent, out var key))
                    keys.Add(key.Value);
            }

            return keys;
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
        private bool FindAccessTagsItem(EntityUid uid, [NotNullWhen(true)] out HashSet<string>? tags)
        {
            if (TryComp(uid, out AccessComponent? access))
            {
                tags = access.Tags;
                return true;
            }

            if (TryComp(uid, out PDAComponent? pda) &&
                pda.ContainedID?.Owner is {Valid: true} id)
            {
                tags = EntityManager.GetComponent<AccessComponent>(id).Tags;
                return true;
            }

            tags = null;
            return false;
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

            if (TryComp<PDAComponent>(uid, out var pda) &&
                pda.ContainedID?.Owner is {Valid: true} id)
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
    }
}

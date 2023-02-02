using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.Emag.Systems;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.MachineLinking.Events;

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
        }

        private void OnLinkAttempt(EntityUid uid, AccessReaderComponent component, LinkAttemptEvent args)
        {
            if (args.User == null) // AutoLink (and presumably future external linkers) have no user.
                return;
            if (component.Enabled && !IsAllowed(args.User.Value, component))
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
            if (reader.Enabled)
            {
                reader.Enabled = false;
                args.Handled = true;
            }
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
            var tags = FindAccessTags(source);
            return IsAllowed(tags, reader);
        }

        /// <summary>
        /// Searches the given entity for access tags
        /// then compares it with the readers access list to see if it is allowed.
        /// </summary>
        /// <param name="entity">The entity that wants access.</param>
        /// <param name="reader">A reader from a different entity</param>
        public bool IsAllowed(EntityUid entity, AccessReaderComponent reader)
        {
            var tags = FindAccessTags(entity);
            return IsAllowed(tags, reader);
        }

        /// <summary>
        /// Compares the given tags with the readers access list to see if it is allowed.
        /// </summary>
        /// <param name="accessTags">A list of access tags</param>
        /// <param name="reader">An access reader to check against</param>
        public bool IsAllowed(ICollection<string> accessTags, AccessReaderComponent reader)
        {
            if (!reader.Enabled)
            {
                // Access reader is totally disabled, so access is always allowed.
                return true;
            }

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
        /// Finds the access tags on the given entity
        /// </summary>
        /// <param name="uid">The entity that is being searched.</param>
        public ICollection<string> FindAccessTags(EntityUid uid)
        {
            HashSet<string>? tags = null;
            var owned = false;

            // check entity itself
            FindAccessTagsItem(uid, ref tags, ref owned);

            FindAccessItemsInventory(uid, out var items);

            var ev = new GetAdditionalAccessEvent
            {
                Entities = items
            };
            RaiseLocalEvent(uid, ref ev);
            foreach (var ent in ev.Entities)
            {
                FindAccessTagsItem(ent, ref tags, ref owned);
            }

            return (ICollection<string>?) tags ?? Array.Empty<string>();
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
            if (EntityManager.TryGetComponent(uid, out AccessComponent? access))
            {
                tags = access.Tags;
                return true;
            }

            if (EntityManager.TryGetComponent(uid, out PDAComponent? pda) &&
                pda.ContainedID?.Owner is {Valid: true} id)
            {
                tags = EntityManager.GetComponent<AccessComponent>(id).Tags;
                return true;
            }

            tags = null;
            return false;
        }
    }
}

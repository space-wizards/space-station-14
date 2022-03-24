using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.Emag.Systems;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;

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

        private void OnEmagged(EntityUid uid, AccessReaderComponent reader, GotEmaggedEvent args)
        {
            if (reader.Enabled == true)
            {
                reader.Enabled = false;
                args.Handled = true;
            }
        }

        /// <summary>
        /// Searches an <see cref="AccessComponent"/> in the entity itself, in its active hand or in its ID slot.
        /// Then compares the found access with the configured access lists to see if it is allowed.
        /// </summary>
        /// <remarks>
        ///     If no access is found, an empty set is used instead.
        /// </remarks>
        /// <param name="entity">The entity to bor access.</param>
        public bool IsAllowed(AccessReaderComponent reader, EntityUid entity)
        {
            var tags = FindAccessTags(entity);
            return IsAllowed(reader, tags);
        }

        public bool IsAllowed(AccessReaderComponent reader, ICollection<string> accessTags)
        {
            if (reader.DenyTags.Overlaps(accessTags))
            {
                // Sec owned by cargo.
                return false;
            }

            return !reader.Enabled || reader.AccessLists.Count == 0 || reader.AccessLists.Any(a => a.IsSubsetOf(accessTags));
        }

        public ICollection<string> FindAccessTags(EntityUid uid)
        {
            // check entity itself
            if (FindAccessTagsItem(uid, out var tags))
                return tags;

            foreach (var item in _handsSystem.EnumerateHeld(uid))
            {
                if (FindAccessTagsItem(item, out tags))
                    return tags;
            }

            // maybe its inside an inventory slot?
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) && FindAccessTagsItem(idUid.Value, out tags))
                return tags;

            return Array.Empty<string>();
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

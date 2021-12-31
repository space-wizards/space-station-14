using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems
{
    public class AccessReaderSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AccessReaderComponent, ComponentInit>(OnInit);
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

        /// <summary>
        /// Searches an <see cref="AccessComponent"/> in the entity itself, in its active hand or in its ID slot.
        /// Then compares the found access with the configured access lists to see if it is allowed.
        /// </summary>
        /// <remarks>
        ///     If no access is found, an empty set is used instead.
        /// </remarks>
        /// <param name="entity">The entity to be searched for access.</param>
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

            // maybe access component inside its hands?
            if (EntityManager.TryGetComponent(uid, out SharedHandsComponent? hands))
            {
                if (hands.TryGetActiveHeldEntity(out var heldItem) &&
                    FindAccessTagsItem(heldItem.Value, out tags))
                {
                    return tags;
                }
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

using Content.Server.Access.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Shared.Access;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Access.Systems
{
    public class AccessReaderSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AccessReader, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, AccessReader reader, ComponentInit args)
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
        public bool IsAllowed(AccessReader reader, EntityUid entity)
        {
            var tags = FindAccessTags(entity);
            return IsAllowed(reader, tags);
        }

        public bool IsAllowed(AccessReader reader, ICollection<string> accessTags)
        {
            if (reader.DenyTags.Overlaps(accessTags))
            {
                // Sec owned by cargo.
                return false;
            }

            return reader.AccessLists.Count == 0 || reader.AccessLists.Any(a => a.IsSubsetOf(accessTags));
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
                    FindAccessTagsItem(heldItem.Uid, out tags))
                {
                    return tags;
                }
            }

            // maybe its inside an inventory slot?
            if (EntityManager.TryGetComponent(uid, out InventoryComponent? inventoryComponent))
            {
                if (inventoryComponent.HasSlot(EquipmentSlotDefines.Slots.IDCARD) &&
                    inventoryComponent.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent? item) &&
                    FindAccessTagsItem(item.Owner.Uid, out tags)
                )
                {
                    return tags;
                }
            }

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

            if (EntityManager.TryGetComponent(uid, out PDAComponent? pda))
            {
                tags = pda?.ContainedID?.Owner?.GetComponent<AccessComponent>()?.Tags;
                return tags != null;
            }

            tags = null;
            return false;
        }
    }
}

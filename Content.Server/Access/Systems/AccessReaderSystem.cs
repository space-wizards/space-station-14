using Content.Server.Access.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.Access;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Access.Systems
{
    public class AccessReaderSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AccessReader, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, AccessReader reader, ComponentInit args)
        {
            var proto = IoCManager.Resolve<IPrototypeManager>();
            var allTags = reader.AccessLists.SelectMany(c => c).Union(reader.DenyTags);
            foreach (var level in allTags)
            {
                if (!proto.HasIndex<AccessLevelPrototype>(level))
                {
                    Logger.ErrorS("access", $"Invalid access level: {level}");
                }
            }
        }

        /// <summary>
        /// Searches an <see cref="IAccess"/> in the entity itself, in its active hand or in its ID slot.
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

        public bool IsAllowed(AccessReader reader, IAccess access)
        {
            return IsAllowed(reader, access.Tags);
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
            if (EntityManager.TryGetComponent(uid, out IAccess? access))
            {
                return access.Tags;
            }

            if (EntityManager.TryGetComponent(uid, out SharedHandsComponent? hands))
            {
                if (hands.TryGetActiveHeldEntity(out var heldItem) &&
                    heldItem.TryGetComponent(out IAccess? handAccessComponent))
                {
                    return handAccessComponent.Tags;
                }
            }

            if (EntityManager.TryGetComponent(uid, out InventoryComponent? inventoryComponent))
            {
                if (inventoryComponent.HasSlot(EquipmentSlotDefines.Slots.IDCARD) &&
                    inventoryComponent.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent? item) &&
                    item.Owner.TryGetComponent(out IAccess? idAccessComponent)
                )
                {
                    return idAccessComponent.Tags;
                }
            }

            return Array.Empty<string>();
        }
    }
}

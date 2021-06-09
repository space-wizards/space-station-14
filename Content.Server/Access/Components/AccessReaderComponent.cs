#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Access;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    /// <summary>
    ///     Stores access levels necessary to "use" an entity
    ///     and allows checking if something or somebody is authorized with these access levels.
    /// </summary>
    [PublicAPI]
    [RegisterComponent]
    public class AccessReader : Component
    {
        public override string Name => "AccessReader";

        private readonly HashSet<string> _denyTags = new();

        /// <summary>
        ///     List of access lists to check allowed against. For an access check to pass
        ///     there has to be an access list that is a subset of the access in the checking list.
        /// </summary>
        [DataField("access")]
        [ViewVariables]
        public List<HashSet<string>> AccessLists { get; } = new();

        /// <summary>
        ///     The set of tags that will automatically deny an allowed check, if any of them are present.
        /// </summary>
        [ViewVariables] public ISet<string> DenyTags => _denyTags;

        /// <summary>
        /// Searches an <see cref="IAccess"/> in the entity itself, in its active hand or in its ID slot.
        /// Then compares the found access with the configured access lists to see if it is allowed.
        /// </summary>
        /// <remarks>
        ///     If no access is found, an empty set is used instead.
        /// </remarks>
        /// <param name="entity">The entity to be searched for access.</param>
        public bool IsAllowed(IEntity entity)
        {
            var tags = FindAccessTags(entity);
            return IsAllowed(tags);
        }

        public bool IsAllowed(IAccess access)
        {
            return IsAllowed(access.Tags);
        }

        public bool IsAllowed(ICollection<string> accessTags)
        {
            if (_denyTags.Overlaps(accessTags))
            {
                // Sec owned by cargo.
                return false;
            }

            return AccessLists.Count == 0 || AccessLists.Any(a => a.IsSubsetOf(accessTags));
        }

        public static ICollection<string> FindAccessTags(IEntity entity)
        {
            if (entity.TryGetComponent(out IAccess? accessComponent))
            {
                return accessComponent.Tags;
            }

            if (entity.TryGetComponent(out IHandsComponent? handsComponent))
            {
                var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                if (activeHandEntity != null &&
                    activeHandEntity.TryGetComponent(out IAccess? handAccessComponent))
                {
                    return handAccessComponent.Tags;
                }
            }
            else
            {
                return Array.Empty<string>();
            }

            if (entity.TryGetComponent(out InventoryComponent? inventoryComponent))
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

        public override void Initialize()
        {
            base.Initialize();

            var proto = IoCManager.Resolve<IPrototypeManager>();
            foreach (var level in AccessLists.SelectMany(c => c).Union(DenyTags))
            {
                if (!proto.HasIndex<AccessLevelPrototype>(level))
                {
                    Logger.ErrorS("access", $"Invalid access level: {level}");
                }
            }
        }
    }
}

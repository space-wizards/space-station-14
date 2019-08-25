using System.Collections.Generic;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class AccessReader : Component
    {
        public override string Name => "AccessReader";
        private List<string> _necessaryTags;
        private List<string> _sufficientTags;

        public bool IsAllowed(IEntity entity)
        {
            var accessProvider = FindAccessProvider(entity);
            return accessProvider != null && IsAllowed(accessProvider);
        }

        private bool IsAllowed(AccessComponent accessProvider)
        {
            foreach (var sufficient in _sufficientTags)
            {
                if (accessProvider.Tags.Contains(sufficient))
                {
                    return true;
                }
            }
            foreach (var necessary in _necessaryTags)
            {
                if (!accessProvider.Tags.Contains(necessary))
                {
                    return false;
                }
            }
            return true;
        }

        [CanBeNull]
        private static AccessComponent FindAccessProvider(IEntity entity)
        {
            if (entity.TryGetComponent(out AccessComponent accessComponent))
            {
                return accessComponent;
            }

            if (entity.TryGetComponent(out IHandsComponent handsComponent))
            {
                var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                if (activeHandEntity != null &&
                    activeHandEntity.TryGetComponent(out AccessComponent handAccessComponent))
                {
                    return handAccessComponent;
                }
            }
            else
            {
                return null;
            }
            if (entity.TryGetComponent(out InventoryComponent inventoryComponent))
            {
                if (inventoryComponent.HasSlot(EquipmentSlotDefines.Slots.IDCARD) &&
                    inventoryComponent.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent item) &&
                    item.Owner.TryGetComponent(out AccessComponent idAccessComponent)
                    )
                {
                    return idAccessComponent;
                }
            }
            return null;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _necessaryTags, "necessary" ,new List<string>());
            serializer.DataField(ref _sufficientTags, "sufficient", new List<string>());
        }
    }
}

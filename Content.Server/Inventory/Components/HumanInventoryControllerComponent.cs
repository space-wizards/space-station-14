using System.Diagnostics.CodeAnalysis;
using Content.Server.Items;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Inventory.Components
{
    // Handles the special behavior of pockets/ID card slot and their relation to uniforms.
    [RegisterComponent]
    [ComponentReference(typeof(IInventoryController))]
    public class HumanInventoryControllerComponent : Component, IInventoryController
    {
        public override string Name => "HumanInventoryController";

        private InventoryComponent _inventory = default!;

        public override void Initialize()
        {
            base.Initialize();

            _inventory = Owner.EnsureComponent<InventoryComponent>();
        }

        bool IInventoryController.CanEquip(Slots slot, IEntity entity, bool flagsCheck, [NotNullWhen(false)] out string? reason)
        {
            var slotMask = SlotMasks[slot];
            reason = null;

            if ((slotMask & (SlotFlags.POCKET | SlotFlags.IDCARD)) != SlotFlags.NONE)
            {
                // Can't wear stuff in ID card or pockets unless you have a uniform.
                if (_inventory.GetSlotItem(Slots.INNERCLOTHING) == null)
                {
                    reason = Loc.GetString(slotMask == SlotFlags.IDCARD
                        ? "You need a uniform to store something in your ID slot!"
                        : "You need a uniform to store something in your pockets!");
                    return false;
                }

                if (slotMask == SlotFlags.POCKET)
                {
                    var itemComponent = entity.GetComponent<ItemComponent>();

                    // If this item is small enough then it always fits in pockets.
                    if (itemComponent.Size <= (int) ReferenceSizes.Pocket)
                    {
                        return true;
                    }
                    else if (!flagsCheck)
                    {
                        reason = Loc.GetString("This is too large!");
                    }
                }
            }

            // Standard flag check.
            return flagsCheck;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case ContainerContentsModifiedMessage contentsModified:
                    Owner.SpawnTimer(0, DropIdAndPocketsIfWeNoLongerHaveAUniform);
                    break;
            }
        }

        // Hey, it's descriptive.
        private void DropIdAndPocketsIfWeNoLongerHaveAUniform()
        {
            if (Deleted)
            {
                return;
            }

            if (_inventory.GetSlotItem(Slots.INNERCLOTHING) != null)
            {
                return;
            }

            void DropMaybe(Slots slot)
            {
                if (_inventory.GetSlotItem(slot) != null)
                {
                    _inventory.Unequip(slot);
                }
            }

            DropMaybe(Slots.POCKET1);
            DropMaybe(Slots.POCKET2);
            DropMaybe(Slots.IDCARD);
        }
    }
}

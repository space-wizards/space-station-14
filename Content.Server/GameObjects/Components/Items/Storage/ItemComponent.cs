using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using System;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects
{
    public class ItemComponent : StoreableComponent, IItemComponent, EntitySystems.IAttackHand
    {
        public override string Name => "Item";

        /// <inheritdoc />
        public IInventorySlot ContainingSlot { get; private set; }

        public void RemovedFromSlot()
        {
            if (ContainingSlot == null)
            {
                throw new InvalidOperationException("Item is not in a slot.");
            }

            ContainingSlot = null;

            foreach (var component in Owner.GetComponents<ISpriteRenderableComponent>())
            {
                component.Visible = true;
            }
        }

        public void EquippedToSlot(IInventorySlot slot)
        {
            if (ContainingSlot != null)
            {
                throw new InvalidOperationException("Item is already in a slot.");
            }

            ContainingSlot = slot;

            foreach (var component in Owner.GetComponents<ISpriteRenderableComponent>())
            {
                component.Visible = false;
            }
        }

        public bool Attackhand(IEntity user)
        {
            if (ContainingSlot != null)
            {
                return false;
            }
            var hands = user.GetComponent<IHandsComponent>();
            hands.PutInHand(this, hands.ActiveIndex, fallback: false);
            return true;
        }
    }
}

using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using System;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects
{
    public class ItemComponent : StoreableComponent, EntitySystems.IAttackHand
    {
        public override string Name => "Item";


        public void RemovedFromSlot()
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = true;
            }
        }

        public void EquippedToSlot(ContainerSlot slot)
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = false;
            }
        }

        public bool Attackhand(IEntity user)
        {
            var hands = user.GetComponent<IHandsComponent>();
            hands.PutInHand(this, hands.ActiveIndex, fallback: false);
            return true;
        }
    }
}

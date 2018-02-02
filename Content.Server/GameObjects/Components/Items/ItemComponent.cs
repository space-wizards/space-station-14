using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Server.GameObjects;
using SS14.Shared.IoC;
using Content.Server.GameObjects.EntitySystems;

namespace Content.Server.GameObjects
{
    public class ItemComponent : Component, IItemComponent, EntitySystems.IAttackHand
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

        public override void Initialize()
        {
            base.Initialize();

            var interactionsystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionsystem.AddEvent(Owner.GetComponent<ClickableComponent>());
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

        public override void Shutdown()
        {
            var interactionsystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionsystem.RemoveEvent(Owner.GetComponent<ClickableComponent>());

            base.Shutdown();
        }
    }
}

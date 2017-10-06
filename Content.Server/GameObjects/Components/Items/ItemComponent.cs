using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using System;

namespace Content.Server.GameObjects
{
    public class ItemComponent : Component, IItemComponent
    {
        public override string Name => "Item";

        /// <inheritdoc />
        public IInventorySlot ContainingSlot { get; private set; }
        private IInteractableComponent interactableComponent;

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
            if (Owner.TryGetComponent<IInteractableComponent>(out var interactable))
            {
                interactableComponent = interactable;
                interactableComponent.OnAttackHand += InteractableComponent_OnAttackHand;
            }
            else
            {
                Logger.Error($"Item component must have an interactable component to function! Prototype: {Owner.Prototype.ID}");
            }
            base.Initialize();
        }

        private void InteractableComponent_OnAttackHand(object sender, AttackHandEventArgs e)
        {
            if (ContainingSlot != null)
            {
                return;
            }
            var hands = e.User.GetComponent<IHandsComponent>();
            hands.PutInHand(this, e.HandIndex, fallback: false);
        }

        public override void Shutdown()
        {
            if (interactableComponent != null)
            {
                interactableComponent.OnAttackHand -= InteractableComponent_OnAttackHand;
                interactableComponent = null;
            }
            base.Shutdown();
        }
    }
}

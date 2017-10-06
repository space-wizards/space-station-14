using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Log;
using System;

namespace Content.Server.GameObjects
{
    public class InteractableComponent : Component, IInteractableComponent
    {
        public override string Name => "Interactable";

        /// <inheritdoc />
        public event EventHandler<AttackHandEventArgs> OnAttackHand;

        /// <inheritdoc />
        public event EventHandler<AttackByEventArgs> OnAttackBy;

        private IClickableComponent clickableComponent;
        private IServerTransformComponent transform;
        private const float INTERACTION_RANGE = 2;
        private const float INTERACTION_RANGE_SQUARED = INTERACTION_RANGE * INTERACTION_RANGE;

        public override void Initialize()
        {
            transform = Owner.GetComponent<IServerTransformComponent>();
            if (Owner.TryGetComponent<IClickableComponent>(out var component))
            {
                clickableComponent = component;
                clickableComponent.OnClick += ClickableComponent_OnClick;
            }
            else
            {
                Logger.Error($"Interactable component must also have a clickable component to function! Prototype: {Owner.Prototype.ID}");
            }
            base.Initialize();
        }

        public override void Shutdown()
        {
            if (clickableComponent != null)
            {
                clickableComponent.OnClick -= ClickableComponent_OnClick;
                clickableComponent = null;
            }
            transform = null;
            base.Shutdown();
        }

        private void ClickableComponent_OnClick(object sender, ClickEventArgs e)
        {
            if (!e.User.TryGetComponent<IServerTransformComponent>(out var userTransform))
            {
                return;
            }

            var distance = (userTransform.WorldPosition - transform.WorldPosition).LengthSquared;
            if (distance > INTERACTION_RANGE_SQUARED)
            {
                return;
            }

            if (!e.User.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetHand(hands.ActiveIndex);
            if (item != null)
            {
                OnAttackBy?.Invoke(this, new AttackByEventArgs(Owner, e.User, item, hands.ActiveIndex));
            }
            else
            {
                OnAttackHand?.Invoke(this, new AttackHandEventArgs(Owner, e.User, hands.ActiveIndex));
            }
        }
    }
}

using System;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AnchorableComponent : Component, IInteractUsing
    {
        public override string Name => "Anchorable";

        public event EventHandler<IEntity> OnAnchor;
        public event EventHandler<IEntity> OnUnAnchor;

        private bool Anchor(IEntity user, IEntity utilizing)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent physics) ||
                !utilizing.TryGetComponent(out ToolComponent tool))
            {
                return false;
            }

            if (!tool.UseTool(user, Owner, ToolQuality.Anchoring))
            {
                return false;
            }

            physics.Anchored = !physics.Anchored;

            if (physics.Anchored)
            {
                OnAnchor?.Invoke(this, Owner);
            }
            else
            {
                OnUnAnchor?.Invoke(this, Owner);
            }

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PhysicsComponent>();
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return Anchor(eventArgs.User, eventArgs.Using);
        }
    }
}

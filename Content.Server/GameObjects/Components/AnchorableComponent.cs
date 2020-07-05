using System;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AnchorableComponent : Component, IInteractUsing
    {
        public override string Name => "Anchorable";

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
                var args = new AnchoredEventArgs();

                foreach (var component in Owner.GetAllComponents<IAnchored>())
                {
                    component.Anchored(args);
                }
            }
            else
            {
                var args = new UnAnchoredEventArgs();

                foreach (var unAnchored in Owner.GetAllComponents<IUnAnchored>())
                {
                    unAnchored.UnAnchored(args);
                }
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

    /// <summary>
    ///     This interface gives components behavior when they're anchored.
    /// </summary>
    public interface IAnchored
    {
        void Anchored(AnchoredEventArgs eventArgs);
    }

    public class AnchoredEventArgs : EventArgs
    {
        public AnchoredEventArgs()
        {
        }
    }

    /// <summary>
    ///     This interface gives components behavior when they're unanchored.
    /// </summary>
    public interface IUnAnchored
    {
        void UnAnchored(UnAnchoredEventArgs eventArgs);
    }

    public class UnAnchoredEventArgs : EventArgs
    {
        public UnAnchoredEventArgs()
        {
        }
    }
}

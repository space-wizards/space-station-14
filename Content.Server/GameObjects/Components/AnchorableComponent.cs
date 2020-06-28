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
                var args = new AnchoredEventArgs(Owner);

                foreach (var component in Owner.GetAllComponents<IAnchored>())
                {
                    component.Anchored(args);
                }
            }
            else
            {
                var args = new UnAnchoredEventArgs(Owner);

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
}

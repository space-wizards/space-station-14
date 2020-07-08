using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AnchorableComponent : Component, IInteractUsing
    {
        public override string Name => "Anchorable";

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PhysicsComponent>();
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent physics)
                || !eventArgs.Using.TryGetComponent(out ToolComponent tool))
                return false;

            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Anchoring))
                return false;

            physics.Anchored = !physics.Anchored;

            return true;
        }
    }
}

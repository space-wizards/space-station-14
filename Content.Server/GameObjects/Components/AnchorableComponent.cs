using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AnchorableComponent : Component, IWrenchAct
    {
        public override string Name => "Anchorable";

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PhysicsComponent>();
        }

        public bool WrenchAct(WrenchActEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent physics))
            {
                return false;
            }

            physics.Anchored = !physics.Anchored;
            eventArgs.ToolComponent.PlayUseSound();

            return true;
        }
    }
}

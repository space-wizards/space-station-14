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
    public class AnchorableComponent : Component, IAttackBy
    {
        public override string Name => "Anchorable";

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PhysicsComponent>();
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out PhysicsComponent physics)
                || !eventArgs.AttackWith.TryGetComponent(out ToolComponent tool))
                return false;

            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Anchoring))
                return false;

            physics.Anchored = !physics.Anchored;

            return true;
        }
    }
}

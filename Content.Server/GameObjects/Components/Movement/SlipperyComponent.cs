using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class SlipperyComponent : Component, ICollideBehavior
    {
        public override string Name => "Slippery";

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<CollidableComponent>();
            Owner.EnsureComponent<PhysicsComponent>();
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (!collidedWith.TryGetComponent(out StunnableComponent stun))
                return;

            stun.Paralyze(5f);
        }
    }
}

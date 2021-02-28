#nullable enable
using Content.Server.GameObjects.EntitySystems.Click;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class ThrownItemComponent : Component, ICollideBehavior
    {
        public override string Name => "ThrownItem";

        public IEntity? Thrower { get; set; }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PhysicsSleepCompMessage:
                    EntitySystem.Get<InteractionSystem>().LandInteraction(Thrower, Owner, Owner.Transform.Coordinates);
                    IoCManager.Resolve<IComponentManager>().RemoveComponent(Owner.Uid, this);
                    break;
            }
        }

        public void CollideWith(IPhysBody ourBody, IPhysBody otherBody)
        {
            EntitySystem.Get<InteractionSystem>().ThrowCollideInteraction(Thrower, ourBody, otherBody);
        }
    }
}

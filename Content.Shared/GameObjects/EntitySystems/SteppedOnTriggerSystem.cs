using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.EntitySystems
{
    public sealed class SteppedOnTriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SteppedOnTriggerComponent, StartCollideEvent>(HandleCollision);
        }

        private void HandleCollision(EntityUid uid, SteppedOnTriggerComponent component, StartCollideEvent args)
        {
            var otherOwner = args.OtherFixture.Body.Owner;
            if (!args.OurFixture.Name.Equals(SteppedOnTriggerComponent.SteppedOnFixture)) return;
            RaiseLocalEvent(uid, new SteppedOnEvent(otherOwner));
        }
    }

    /// <summary>
    /// Raised if this entity has a SteppedOnTrigger tag and is collided with.
    /// </summary>
    public sealed class SteppedOnEvent : EntityEventArgs
    {
        public IEntity By { get; }

        public SteppedOnEvent(IEntity by)
        {
            By = by;
        }
    }
}

using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.EntitySystems
{
    public sealed class SteppedOnTriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TagComponent, StartCollideEvent>(HandleCollision);
        }

        private void HandleCollision(EntityUid uid, TagComponent component, StartCollideEvent args)
        {
            if (!component.HasTag("SteppedOnTrigger")) return;
            RaiseLocalEvent(uid, new SteppedOnEvent());
        }
    }

    /// <summary>
    /// Raised if this entity has a SteppedOnTrigger tag and is collided with.
    /// </summary>
    public sealed class SteppedOnEvent : EntityEventArgs {}
}

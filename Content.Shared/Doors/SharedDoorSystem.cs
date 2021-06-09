using Content.Shared.GameObjects.Components.Doors;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedDoorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedDoorComponent, PreventCollideEvent>(PreventCollision);
        }

        private void PreventCollision(EntityUid uid, SharedDoorComponent component, PreventCollideEvent args)
        {
            if (component.IsCrushing(args.BodyB.Owner))
            {
                args.Cancel();
            }
        }
    }
}
